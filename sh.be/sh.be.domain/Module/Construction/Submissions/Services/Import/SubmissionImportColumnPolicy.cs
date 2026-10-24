using sh.be.domain.Module.Construction.DocumentFields.Services;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Submissions.Services.Import.Models;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import
{
    public static class SubmissionImportColumnPolicy
    {
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
            IReadOnlyList<ProjectDocumentTypeDocumentField> mappings)
        {
            var orderedMappings = mappings
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.FieldImport))
                .OrderBy(mapping => mapping.OrderFieldImport)
                .ThenBy(mapping => mapping.DocumentFieldId)
                .ToList();
            var headerIndexesByKey = headers
                .Select((header, index) => (Key: FieldImportMatchPolicy.ToMatchKey(header), Index: index))
                .Where(header => header.Key.Length > 0)
                .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Select(header => header.Index).ToList(), StringComparer.OrdinalIgnoreCase);
            var duplicatedHeaderMessages = orderedMappings
                .GroupBy(mapping => FieldImportMatchPolicy.ToMatchKey(mapping.FieldImport), StringComparer.OrdinalIgnoreCase)
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
                var key = FieldImportMatchPolicy.ToMatchKey(mapping.FieldImport);
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
                if (FieldImportMatchPolicy.IsMatch(columnName, headers[index]))
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
    }
}
