using sh.be.domain.Base.Models;
using sh.be.domain.Module.Construction.DocumentFields.Services;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text;

namespace sh.be.domain.Module.Construction.Submissions.Services
{
    public static class SubmissionImportDomainService
    {
        private const int AlphabetLength = 26;
        private const int ExcelFileKeyPartCount = 4;
        private const int ExcelFileKeyUserIdIndex = 1;

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

        public static AppendImportPlan BuildAppendPlan(SheetTable table, SubmissionImportColumns columns, List<MappedImportColumn> mappedColumns,
            List<ExistingSubmissionSnapshot> snapshots, int projectId, int documentTypeId, string? importedBy, DateTime importedDate)
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
                            BuildSheetCellAddress(column.ColumnIndex, row.SheetRowNumber), importedBy, importedDate))
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

        public static BlankImportPlan BuildBlankPlan(SheetTable table, SubmissionImportColumns columns, List<MappedImportColumn> mappedColumns,
            List<ExistingSubmissionSnapshot> snapshots, List<SubmissionData> existingDatas, int projectId, int documentTypeId,
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

                    var indexGoogleSheet = BuildSheetCellAddress(column.ColumnIndex, row.SheetRowNumber);
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
            ProjectDocumentTypeDocumentField mapping, List<ExistingSubmissionSnapshot> snapshots, List<SubmissionData> existingDatas,
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

        public static SubmissionImportColumns ResolveColumns(IReadOnlyList<string> headers, string fileColumn, string? profileRangeColumn,
            string? boxColumn, string? fileUrlColumn)
        {
            EnsureColumnNameEntered(fileColumn, "tên file");

            var missingColumns = new List<string>();
            var fileNameColumnIndex = FindColumn(headers, fileColumn, missingColumns);
            var profileRangeColumnIndex = FindColumn(headers, profileRangeColumn, missingColumns);
            var boxColumnIndex = FindColumn(headers, boxColumn, missingColumns);
            var fileUrlColumnIndex = FindColumn(headers, fileUrlColumn, missingColumns);
            EnsureNoMissingColumns(missingColumns);

            return new SubmissionImportColumns(fileNameColumnIndex!.Value, profileRangeColumnIndex, boxColumnIndex, fileUrlColumnIndex);
        }

        public static int ResolveSingleColumn(IReadOnlyList<string> headers, string columnName)
        {
            EnsureColumnNameEntered(columnName, "dữ liệu");

            var missingColumns = new List<string>();
            var columnIndex = FindColumn(headers, columnName, missingColumns);
            EnsureNoMissingColumns(missingColumns);
            return columnIndex!.Value;
        }

        public static List<MappedImportColumn> ResolveMappedColumns(IReadOnlyList<string> headers,
            List<ProjectDocumentTypeDocumentField> mappings)
        {
            var orderedMappings = mappings
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.FieldImport))
                .OrderBy(mapping => mapping.OrderFieldImport)
                .ThenBy(mapping => mapping.DocumentFieldId)
                .ToList();
            var headerIndexesByKey = headers
                .Select((header, index) => (Key: DocumentFieldDomainService.ToFieldImportMatchKey(header), Index: index))
                .Where(header => header.Key.Length > 0)
                .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(header => header.Index).ToList(), StringComparer.OrdinalIgnoreCase);
            var duplicatedHeaderMessages = orderedMappings
                .GroupBy(mapping => DocumentFieldDomainService.ToFieldImportMatchKey(mapping.FieldImport), StringComparer.OrdinalIgnoreCase)
                .Select(group => (MappingCount: group.Count(), HeaderIndexes: headerIndexesByKey.GetValueOrDefault(group.Key)))
                .Where(group => group.HeaderIndexes != null && group.HeaderIndexes.Count > group.MappingCount)
                .Select(group => $"Cột {headers[group.HeaderIndexes![0]]} xuất hiện {group.HeaderIndexes.Count} lần nhưng chỉ cấu hình {group.MappingCount} trường")
                .ToList();
            if (duplicatedHeaderMessages.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportNotEnoughFieldMapping, string.Join(" | ", duplicatedHeaderMessages));
            }

            var mappedColumns = new List<MappedImportColumn>();
            var usedHeaderCountByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in orderedMappings)
            {
                var key = DocumentFieldDomainService.ToFieldImportMatchKey(mapping.FieldImport);
                var usedHeaderCount = usedHeaderCountByKey.GetValueOrDefault(key);
                if (!headerIndexesByKey.TryGetValue(key, out var headerIndexes) || usedHeaderCount >= headerIndexes.Count)
                {
                    continue;
                }

                usedHeaderCountByKey[key] = usedHeaderCount + 1;
                mappedColumns.Add(new MappedImportColumn(mapping.DocumentFieldId, headerIndexes[usedHeaderCount], mappedColumns.Count + 1));
            }
            return mappedColumns;
        }

        public static string BuildSubmissionKey(string fileName, string profileRange, string box)
        {
            return string.Join(SubmissionConst.KEY_SEPARATOR,
                fileName.Trim().ToLowerInvariant(),
                profileRange.Trim().ToLowerInvariant(),
                box.Trim().ToLowerInvariant());
        }

        public static string BuildSheetCellAddress(int columnIndex, int sheetRowNumber)
        {
            return $"{GetColumnLetter(columnIndex)}{sheetRowNumber}";
        }

        public static void EnsureValidExcelFile(string fileName, long length)
        {
            if (!fileName.EndsWith(SubmissionConst.IMPORT_EXCEL_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid, "Chỉ nhận file Excel .xlsx");
            }
            if (length <= 0 || length > SubmissionConst.IMPORT_EXCEL_MAX_BYTES)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid,
                    $"File rỗng hoặc vượt quá {SubmissionConst.IMPORT_EXCEL_MAX_BYTES / 1024 / 1024} MB");
            }
        }

        public static void EnsureAnySheetHasHeader(List<List<string>> sheetHeaders)
        {
            if (!sheetHeaders.Any(headers => headers.Count > 0))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid, "File không có sheet nào có dòng tiêu đề");
            }
        }

        // Key chứa userId để chỉ người tải mới đọc lại được file; tên file gốc (đã làm sạch) nằm cuối key để job và audit lấy lại tên.
        public static string BuildExcelFileKey(string userId, DateTime uploadedDate, string fileName)
        {
            var safeFileName = string.Join("_", fileName.Split([.. Path.GetInvalidFileNameChars(), '/', '\\'])).Trim();
            return $"{SubmissionConst.IMPORT_OBJECT_KEY_PREFIX}/{uploadedDate:yyyyMMdd}/{userId}/{Guid.NewGuid():N}/{safeFileName}";
        }

        public static string GetExcelFileName(string fileKey)
        {
            return fileKey[(fileKey.LastIndexOf('/') + 1)..];
        }

        // fileKey đi qua FE: chỉ nhận đúng dạng {prefix}/{yyyyMMdd}/{userId}/{guid}/{tên file} của chính người gọi, không cho đoán key đọc file người khác.
        public static void EnsureExcelFileOwnedBy(string fileKey, string userId)
        {
            var keyPrefix = $"{SubmissionConst.IMPORT_OBJECT_KEY_PREFIX}/";
            var keyParts = fileKey.StartsWith(keyPrefix, StringComparison.Ordinal) ? fileKey[keyPrefix.Length..].Split('/') : [];
            if (keyParts.Length != ExcelFileKeyPartCount || keyParts[ExcelFileKeyUserIdIndex] != userId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid, "File không tồn tại hoặc đã hết hạn");
            }
        }

        public static void EnsureExcelRowLimit(SheetTable table)
        {
            if (table.Rows.Count > SubmissionConst.IMPORT_EXCEL_MAX_ROWS)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportFileInvalid,
                    $"Sheet có {table.Rows.Count} dòng dữ liệu, vượt quá giới hạn {SubmissionConst.IMPORT_EXCEL_MAX_ROWS} dòng");
            }
        }

        private static string GetColumnLetter(int columnIndex)
        {
            var columnLetter = new StringBuilder();
            var columnNumber = columnIndex + 1;
            while (columnNumber > 0)
            {
                var letterIndex = (columnNumber - 1) % AlphabetLength;
                columnLetter.Insert(0, (char)('A' + letterIndex));
                columnNumber = (columnNumber - 1) / AlphabetLength;
            }
            return columnLetter.ToString();
        }

        private static void EnsureColumnNameEntered(string? columnName, string columnDescription)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportColumnNotFound, $"Chưa nhập tên cột {columnDescription}");
            }
        }

        private static int? FindColumn(IReadOnlyList<string> headers, string? columnName, List<string> missingColumns)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return null;
            }

            for (var index = 0; index < headers.Count; index++)
            {
                if (DocumentFieldDomainService.IsFieldImportMatch(columnName, headers[index]))
                {
                    return index;
                }
            }

            missingColumns.Add(columnName.Trim());
            return null;
        }

        private static void EnsureNoMissingColumns(List<string> missingColumns)
        {
            if (missingColumns.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportColumnNotFound,
                    $"Không tìm thấy cột: {string.Join(", ", missingColumns)}");
            }
        }

        private static IEnumerable<SheetRow> GetRowsWithFileName(SheetTable table, SubmissionImportColumns columns)
        {
            return table.Rows.Where(row => columns.GetFileName(row).Length > 0);
        }

        private static Dictionary<string, ExistingSubmissionSnapshot> ToSnapshotsByKey(List<ExistingSubmissionSnapshot> snapshots)
        {
            return snapshots
                .GroupBy(snapshot => snapshot.BuildKey())
                .ToDictionary(group => group.Key, group => group.MinBy(snapshot => snapshot.Id)!);
        }

        private static Dictionary<(int SubmissionId, int DocumentFieldId), SubmissionData> ToDatasByField(List<SubmissionData> datas)
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

    public record SubmissionImportColumns(int FileNameColumnIndex, int? ProfileRangeColumnIndex, int? BoxColumnIndex, int? FileUrlColumnIndex)
    {
        public string GetFileName(SheetRow row)
        {
            return row.GetCell(FileNameColumnIndex);
        }

        public string GetProfileRange(SheetRow row)
        {
            return GetOptionalCell(row, ProfileRangeColumnIndex);
        }

        public string GetBox(SheetRow row)
        {
            return GetOptionalCell(row, BoxColumnIndex);
        }

        public string GetFileUrl(SheetRow row)
        {
            return GetOptionalCell(row, FileUrlColumnIndex);
        }

        public string BuildKey(SheetRow row)
        {
            return SubmissionImportDomainService.BuildSubmissionKey(GetFileName(row), GetProfileRange(row), GetBox(row));
        }

        private static string GetOptionalCell(SheetRow row, int? columnIndex)
        {
            return columnIndex.HasValue ? row.GetCell(columnIndex.Value) : String.Empty;
        }
    }

    public record MappedImportColumn(int DocumentFieldId, int ColumnIndex, int IndexRow);

    public record ExistingSubmissionSnapshot(int Id, string FileName, string ProfileRange, string Box, int StatusCheck)
    {
        public string BuildKey()
        {
            return SubmissionImportDomainService.BuildSubmissionKey(FileName, ProfileRange, Box);
        }

        public bool IsChecked()
        {
            return StatusCheck == SubmissionConst.CHECKED;
        }
    }

    public record NewSubmissionDraft(Submission Submission, List<SubmissionData> Datas)
    {
        public List<SubmissionData> AssignDatasToSubmission()
        {
            foreach (var data in Datas)
            {
                data.AssignSubmission(Submission.Id);
            }
            return Datas;
        }
    }

    public record AppendImportPlan(List<NewSubmissionDraft> NewSubmissions, int MappedFieldCount, int NewDataCount,
        int NonEmptyCellCount, int ExistingSubmissionCount, int DistinctSheetKeyCount)
    {
        public bool HasChanges => NewSubmissions.Count > 0;
    }

    public record BlankImportPlan(List<SubmissionData> DatasToUpdate, List<SubmissionData> DatasToInsert, int MappedFieldCount)
    {
        public int ChangedDataCount => DatasToUpdate.Count + DatasToInsert.Count;

        public bool HasChanges => ChangedDataCount > 0;
    }

    public record OverwriteChange(int SubmissionId, string FileName, int DocumentFieldId, string OldData, string NewData);

    public record OverwriteImportPlan(List<SubmissionData> DatasToUpdate, List<SubmissionData> DatasToInsert,
        List<OverwriteChange> Changes, int SkippedCheckedCount)
    {
        public int ChangedDataCount => DatasToUpdate.Count + DatasToInsert.Count;

        public bool HasChanges => ChangedDataCount > 0;
    }
}
