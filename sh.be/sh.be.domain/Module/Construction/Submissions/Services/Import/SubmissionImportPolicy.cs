using sh.be.domain.Base.Models;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Construction.Submissions.Services.Import.Models;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import
{
    public static class SubmissionImportPolicy
    {
        public static void EnsureHasData(SheetTable table)
        {
            if (table.Rows.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportSheetEmpty);
            }
        }

        public static void EnsureNoDuplicateKeys(SheetTable table, SubmissionImportColumns columns)
        {
            var duplicatedKeyMessages = GetRowsWithFileName(table, columns)
                .GroupBy(columns.BuildKey)
                .Where(group => group.Count() > 1)
                .Select(group =>
                {
                    var firstRow = group.First();
                    var sheetRowNumbers = string.Join(", ", group.Select(row => row.SheetRowNumber));
                    return $"Tên file: {columns.GetFileName(firstRow)}, Hộp: {columns.GetBox(firstRow)}, Hồ sơ: {columns.GetProfileRange(firstRow)} "
                        + $"xuất hiện {group.Count()} lần (dòng {sheetRowNumbers})";
                })
                .ToList();

            if (duplicatedKeyMessages.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportDuplicateSubmissionKey, string.Join(" | ", duplicatedKeyMessages));
            }
        }

        public static void EnsureFileUrlsPresent(SheetTable table, SubmissionImportColumns columns)
        {
            if (!columns.FileUrlColumnIndex.HasValue)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportColumnNotFound, "Chưa nhập tên cột link file");
            }

            var missingFileUrlRowNumbers = GetRowsWithFileName(table, columns)
                .Where(row => columns.GetFileUrl(row).Length == 0)
                .Select(row => row.SheetRowNumber)
                .ToList();

            if (missingFileUrlRowNumbers.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileUrlMissing,
                    $"Thiếu link file ở dòng: {string.Join(", ", missingFileUrlRowNumbers)}");
            }
        }

        public static AppendImportPlan BuildAppendPlan(SheetTable table, SubmissionImportColumns columns, IReadOnlyList<MappedImportColumn> mappedColumns,
            IReadOnlyList<ExistingSubmissionSnapshot> snapshots, int projectId, int documentTypeId, string? importedBy, DateTime importedDate)
        {
            var existingKeys = snapshots.Select(snapshot => snapshot.BuildKey()).ToHashSet();

            var newSubmissions = MapRows(table.Rows, row =>
                {
                    var fileName = columns.GetFileName(row);
                    if (fileName.Length == 0 || existingKeys.Contains(columns.BuildKey(row)))
                    {
                        return null;
                    }

                    var submission = Submission.Create(projectId, documentTypeId, fileName, columns.GetProfileRange(row), columns.GetBox(row),
                        columns.GetFileUrl(row), importedBy, importedDate);
                    var datas = mappedColumns
                        .Select(column => submission.CreateData(column.DocumentFieldId, row.GetCell(column.ColumnIndex), column.IndexRow,
                            SheetCellAddressPolicy.BuildAddress(column.ColumnIndex, row.SheetRowNumber), importedBy, importedDate))
                        .ToList();
                    return new NewSubmissionDraft(submission, datas);
                })
                .OfType<NewSubmissionDraft>()
                .ToList();

            var newDatas = newSubmissions.SelectMany(draft => draft.Datas).ToList();
            var distinctSheetKeyCount = GetRowsWithFileName(table, columns).Select(columns.BuildKey).Distinct().Count();

            return new AppendImportPlan(newSubmissions, mappedColumns.Count, newDatas.Count, newDatas.Count(data => data.Data.Length > 0),
                snapshots.Count, distinctSheetKeyCount);
        }

        public static BlankImportPlan BuildBlankPlan(SheetTable table, SubmissionImportColumns columns, IReadOnlyList<MappedImportColumn> mappedColumns,
            IReadOnlyList<ExistingSubmissionSnapshot> snapshots, IReadOnlyList<SubmissionData> existingDatas, int projectId, int documentTypeId,
            string? importedBy, DateTime importedDate)
        {
            var snapshotsByKey = ToSnapshotsByKey(snapshots);
            var existingDatasByField = ToDatasByField(existingDatas);

            var rowChanges = MapRows(table.Rows, row =>
            {
                var changes = new RowDataChanges();
                if (!TryFindSnapshot(row, columns, snapshotsByKey, out var snapshot))
                {
                    return changes;
                }

                foreach (var column in mappedColumns)
                {
                    var value = row.GetCell(column.ColumnIndex);
                    if (value.Length == 0)
                    {
                        continue;
                    }

                    var indexGoogleSheet = SheetCellAddressPolicy.BuildAddress(column.ColumnIndex, row.SheetRowNumber);
                    if (existingDatasByField.TryGetValue((snapshot.Id, column.DocumentFieldId), out var existingData))
                    {
                        if (existingData.FillIfBlank(value, indexGoogleSheet, importedBy, importedDate))
                        {
                            changes.DatasToUpdate.Add(existingData);
                        }
                        continue;
                    }

                    changes.DatasToInsert.Add(SubmissionData.Create(snapshot.Id, projectId, documentTypeId, column.DocumentFieldId, value,
                        column.IndexRow, indexGoogleSheet, importedBy, importedDate));
                }
                return changes;
            });

            return new BlankImportPlan(
                rowChanges.SelectMany(changes => changes.DatasToUpdate).ToList(),
                rowChanges.SelectMany(changes => changes.DatasToInsert).ToList(),
                mappedColumns.Count);
        }

        public static OverwriteImportPlan BuildOverwritePlan(SheetTable table, SubmissionImportColumns columns, int columnImportIndex,
            ProjectDocumentTypeDocumentField mapping, IReadOnlyList<ExistingSubmissionSnapshot> snapshots, IReadOnlyList<SubmissionData> existingDatas,
            bool overwriteChecked, string? importedBy, DateTime importedDate)
        {
            var snapshotsByKey = ToSnapshotsByKey(snapshots);
            var existingDatasByField = ToDatasByField(existingDatas);

            var rowChanges = MapRows(table.Rows, row =>
            {
                var changes = new RowDataChanges();
                var value = row.GetCell(columnImportIndex);
                if (value.Length == 0 || !TryFindSnapshot(row, columns, snapshotsByKey, out var snapshot))
                {
                    return changes;
                }

                if (snapshot.IsChecked() && !overwriteChecked)
                {
                    changes.IsSkippedChecked = true;
                    return changes;
                }

                if (existingDatasByField.TryGetValue((snapshot.Id, mapping.DocumentFieldId), out var existingData))
                {
                    var oldData = existingData.Data;
                    if (existingData.Overwrite(value, importedBy, importedDate))
                    {
                        changes.DatasToUpdate.Add(existingData);
                        changes.Changes.Add(new OverwriteChange(snapshot.Id, snapshot.FileName, mapping.DocumentFieldId, oldData, existingData.Data));
                    }
                    return changes;
                }

                var newData = SubmissionData.Create(snapshot.Id, mapping.ProjectId, mapping.DocumentTypeId, mapping.DocumentFieldId, value,
                    mapping.OrderFieldImport, null, importedBy, importedDate);
                changes.DatasToInsert.Add(newData);
                changes.Changes.Add(new OverwriteChange(snapshot.Id, snapshot.FileName, mapping.DocumentFieldId, String.Empty, newData.Data));
                return changes;
            });

            return new OverwriteImportPlan(
                rowChanges.SelectMany(changes => changes.DatasToUpdate).ToList(),
                rowChanges.SelectMany(changes => changes.DatasToInsert).ToList(),
                rowChanges.SelectMany(changes => changes.Changes).ToList(),
                rowChanges.Count(changes => changes.IsSkippedChecked));
        }

        private static IEnumerable<SheetRow> GetRowsWithFileName(SheetTable table, SubmissionImportColumns columns)
        {
            return table.Rows.Where(row => columns.GetFileName(row).Length > 0);
        }

        private static Dictionary<string, ExistingSubmissionSnapshot> ToSnapshotsByKey(IReadOnlyList<ExistingSubmissionSnapshot> snapshots)
        {
            return snapshots
                .GroupBy(snapshot => snapshot.BuildKey())
                .ToDictionary(group => group.Key, group => group.MinBy(snapshot => snapshot.Id)!);
        }

        private static Dictionary<(int SubmissionId, int DocumentFieldId), SubmissionData> ToDatasByField(IReadOnlyList<SubmissionData> datas)
        {
            return datas
                .GroupBy(data => (data.SubmissionId, data.DocumentFieldId))
                .ToDictionary(group => group.Key, group => group.MinBy(data => data.Id)!);
        }

        private static bool TryFindSnapshot(SheetRow row, SubmissionImportColumns columns,
            Dictionary<string, ExistingSubmissionSnapshot> snapshotsByKey, out ExistingSubmissionSnapshot snapshot)
        {
            snapshot = null!;
            return columns.GetFileName(row).Length > 0 && snapshotsByKey.TryGetValue(columns.BuildKey(row), out snapshot!);
        }

        private static TResult[] MapRows<TResult>(IReadOnlyList<SheetRow> rows, Func<SheetRow, TResult> mapRow)
        {
            var results = new TResult[rows.Count];
            if (rows.Count >= SubmissionConst.PARALLEL_ROW_THRESHOLD)
            {
                Parallel.For(0, rows.Count, index => results[index] = mapRow(rows[index]));
                return results;
            }

            for (var index = 0; index < rows.Count; index++)
            {
                results[index] = mapRow(rows[index]);
            }
            return results;
        }

        private class RowDataChanges
        {
            public List<SubmissionData> DatasToUpdate { get; } = new();
            public List<SubmissionData> DatasToInsert { get; } = new();
            public List<OverwriteChange> Changes { get; } = new();
            public bool IsSkippedChecked { get; set; }
        }
    }
}
