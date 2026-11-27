using sh.be.domain.Base.Models;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.Packaging.Entities;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Packaging.Services
{
    public static class PackageSessionDomainService
    {
        private static readonly string[] FullDateFormats = ["dd/MM/yyyy"];
        private static readonly string[] ShortDateFormats = ["dd/MM/yyyy", "MM/yyyy", "yyyy"];

        public static void EnsureValidSessionMetadataTypes(List<int> metadataTypes)
        {
            PackageSchemaDomainService.EnsureValidMetadataTypes(metadataTypes);
            if (metadataTypes.Distinct().Count() != metadataTypes.Count
                || !metadataTypes.Contains(PackageSchemaConst.METADATA_PACKAGE)
                || metadataTypes.Count < 2)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorMetadataTypeInvalid);
            }
        }

        public static List<string> NormalizeUploadPaths(int fileCount, List<string> relativePaths)
        {
            if (fileCount == 0 || fileCount > PackageSessionConst.MAX_FILES_PER_UPLOAD)
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorUploadFileCountInvalid);
            }

            var normalizedPaths = relativePaths
                .Select(path => string.Join('/', path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Normalize(NormalizationForm.FormC))
                .ToList();
            if (normalizedPaths.Count != fileCount || normalizedPaths.Any(path => !path.Contains('/') || path.Split('/').Contains("..")))
            {
                throw new UserFriendlyException(ErrorCodes.PackagingErrorRelativePathsRequired);
            }
            return normalizedPaths;
        }

        public static PackageUploadCheck CheckUploadFile(int packageType, List<int> metadataTypes, string relativePath, long sizeBytes)
        {
            var fileName = relativePath[(relativePath.LastIndexOf('/') + 1)..];
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension == PackageSessionConst.EXCEL_EXTENSION)
            {
                return new PackageUploadCheck(relativePath, fileName, extension, PackageSessionConst.FILE_ROLE_EXCEL, null, false);
            }
            if (extension == PackageSessionConst.LEGACY_EXCEL_EXTENSION)
            {
                return new PackageUploadCheck(relativePath, fileName, extension, null, "Excel định dạng cũ, lưu lại thành .xlsx", false);
            }

            var acceptedExtensions = metadataTypes
                .Where(PackageSessionConst.DATA_EXTENSIONS.ContainsKey)
                .SelectMany(metadataType => PackageSessionConst.DATA_EXTENSIONS[metadataType])
                .ToHashSet();
            if (packageType == PackageConst.DIP)
            {
                acceptedExtensions.Add(PackageSessionConst.PDF_EXTENSION);
            }
            if (!acceptedExtensions.Contains(extension))
            {
                return new PackageUploadCheck(relativePath, fileName, extension, null, $"Đuôi \"{extension}\" không thuộc loại tài liệu của phiên", true);
            }

            var isMedia = PackageSessionConst.DATA_EXTENSIONS[PackageSchemaConst.METADATA_MEDIA].Contains(extension);
            var maxBytes = isMedia ? PackageSessionConst.MAX_MEDIA_BYTES : PackageSessionConst.MAX_DOCUMENT_BYTES;
            return sizeBytes > maxBytes
                ? new PackageUploadCheck(relativePath, fileName, extension, null, $"File vượt {maxBytes / 1024 / 1024} MB", false)
                : new PackageUploadCheck(relativePath, fileName, extension, PackageSessionConst.FILE_ROLE_DATA, null, false);
        }

        public static string BuildSourceObjectKey(int sessionId, int sequence, string extension)
        {
            return $"{BuildSessionObjectKeyPrefix(sessionId)}source/{sequence:D6}{extension}";
        }

        public static string BuildSessionObjectKeyPrefix(int sessionId)
        {
            return $"{PackageSessionConst.OBJECT_KEY_PREFIX}/{sessionId}/";
        }

        public static string GetFolder(string relativePath)
        {
            var separatorIndex = relativePath.LastIndexOf('/');
            return separatorIndex < 0 ? String.Empty : relativePath[..separatorIndex];
        }

        public static string NormalizeHeader(string header)
        {
            var withoutParentheses = Regex.Replace(header, @"\([^)]*\)", " ");
            var decomposed = withoutParentheses.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
            }
            return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        public static double ScoreHeader(string normalizedHeader, string normalizedDisplayName)
        {
            if (normalizedHeader.Length == 0 || normalizedDisplayName.Length == 0)
            {
                return 0;
            }

            var previous = Enumerable.Range(0, normalizedDisplayName.Length + 1).ToArray();
            for (var i = 1; i <= normalizedHeader.Length; i++)
            {
                var current = new int[normalizedDisplayName.Length + 1];
                current[0] = i;
                for (var j = 1; j <= normalizedDisplayName.Length; j++)
                {
                    var cost = normalizedHeader[i - 1] == normalizedDisplayName[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }
                previous = current;
            }
            var levenshtein = 1 - (double)previous[normalizedDisplayName.Length] / Math.Max(normalizedHeader.Length, normalizedDisplayName.Length);

            var headerWords = normalizedHeader.Split(' ').ToHashSet();
            var displayNameWords = normalizedDisplayName.Split(' ').ToHashSet();
            var dice = 2.0 * headerWords.Intersect(displayNameWords).Count() / (headerWords.Count + displayNameWords.Count);
            return Math.Max(levenshtein, dice);
        }

        public static PackageSheetMatch MatchSheetColumns(PackageVerifySchema schema, IReadOnlyList<string> headers)
        {
            var candidates = new List<(int ColumnIndex, PackageSchemaField Field, double Score)>();
            for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                var normalizedHeader = NormalizeHeader(headers[columnIndex]);
                foreach (var field in schema.Fields)
                {
                    var score = ScoreHeader(normalizedHeader, NormalizeHeader(field.DisplayName));
                    if (score >= PackageSessionConst.HEADER_MATCH_THRESHOLD)
                    {
                        candidates.Add((columnIndex, field, score));
                    }
                }
            }

            var columns = new Dictionary<string, int>();
            var matched = new List<PackageMatchedColumn>();
            var usedColumnIndexes = new HashSet<int>();
            foreach (var candidate in candidates.OrderByDescending(c => c.Score).ThenBy(c => c.ColumnIndex))
            {
                if (columns.ContainsKey(candidate.Field.ElementName) || !usedColumnIndexes.Add(candidate.ColumnIndex))
                {
                    continue;
                }
                columns[candidate.Field.ElementName] = candidate.ColumnIndex;
                matched.Add(new PackageMatchedColumn(headers[candidate.ColumnIndex], candidate.Field.ElementName, candidate.Field.DisplayName, Math.Round(candidate.Score, 2)));
            }

            var unmatchedFields = schema.Fields.Where(f => !columns.ContainsKey(f.ElementName)).ToList();
            return new PackageSheetMatch(schema, columns, new PackageSheetReport(
                schema.Name,
                schema.MetadataType,
                matched,
                unmatchedFields.Select(f => f.DisplayName).ToList(),
                unmatchedFields.Where(f => f.Requirement == PackageSchemaFieldConst.REQUIRED).Select(f => f.DisplayName).ToList(),
                headers.Where((header, index) => header.Length > 0 && !usedColumnIndexes.Contains(index)).ToList()));
        }

        public static string ResolveDocKey(PackageSheetMatch match, SheetRow row)
        {
            var elementNames = match.Schema.Fields.Select(f => f.ElementName).ToHashSet();
            if (elementNames.Contains(PackageSessionConst.DOC_ID))
            {
                return match.GetCell(row, PackageSessionConst.DOC_ID);
            }
            if (elementNames.Contains(PackageSessionConst.DOC_CODE))
            {
                return match.GetCell(row, PackageSessionConst.DOC_CODE);
            }
            if (elementNames.Contains(PackageSessionConst.ARC_DOC_CODE))
            {
                return match.GetCell(row, PackageSessionConst.ARC_DOC_CODE);
            }

            var arcFileCode = match.GetCell(row, PackageSessionConst.ARC_FILE_CODE);
            var docOrdinal = match.GetCell(row, PackageSessionConst.DOC_ORDINAL);
            return arcFileCode.Length == 0 || docOrdinal.Length == 0
                ? String.Empty
                : $"{arcFileCode}.{docOrdinal.PadLeft(PackageSessionConst.DOC_ORDINAL_LENGTH, '0')}";
        }

        public static List<string> ValidateRow(int packageType, PackageSheetMatch match, SheetRow row, Dictionary<string, HashSet<string>> codesByGroup, string? packageCode)
        {
            var errors = new List<string>();
            foreach (var field in match.Schema.Fields)
            {
                var label = $"{field.DisplayName} ({field.ElementName})";
                var value = match.GetCell(row, field.ElementName);
                if (value.Length == 0)
                {
                    var isRequired = field.Requirement == PackageSchemaFieldConst.REQUIRED
                        || (field.Requirement == PackageSchemaFieldConst.CONDITIONAL
                            && field.ConditionElement != null
                            && (field.ConditionValues ?? String.Empty).Split(',', StringSplitOptions.TrimEntries)
                                .Contains(match.GetCell(row, field.ConditionElement)));
                    if (isRequired)
                    {
                        errors.Add($"{label} bắt buộc nhập");
                    }
                    continue;
                }

                if (field.MaxLength.HasValue && value.Length > field.MaxLength.Value)
                {
                    errors.Add($"{label} vượt {field.MaxLength} ký tự");
                }
                if (field.DataType == PackageSchemaFieldConst.DATA_TYPE_NUMBER && !value.All(char.IsAsciiDigit))
                {
                    errors.Add($"{label} chỉ nhận chữ số: \"{value}\"");
                }
                if (field.DataType == PackageSchemaFieldConst.DATA_TYPE_BOOLEAN && value != "1" && value != "0")
                {
                    errors.Add($"{label} chỉ nhận 1 hoặc 0: \"{value}\"");
                }
                if (field.DataType == PackageSchemaFieldConst.DATA_TYPE_DATE)
                {
                    var isShortDateAllowed = PackageSessionConst.SHORT_DATE_PACKAGE_TYPES.Contains(packageType)
                        && PackageSessionConst.SHORT_DATE_ELEMENTS.Contains(field.ElementName);
                    var formats = isShortDateAllowed ? ShortDateFormats : FullDateFormats;
                    if (!DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        errors.Add($"{label} sai định dạng {string.Join(", ", formats)}: \"{value}\"");
                    }
                }
                if (field.CodeGroup != null)
                {
                    var codes = codesByGroup.GetValueOrDefault(field.CodeGroup) ?? new HashSet<string>();
                    var values = field.AllowMultipleValues ? value.Split(',', StringSplitOptions.TrimEntries) : new[] { value };
                    var invalidValues = values.Where(v => !codes.Contains(v)).ToList();
                    if (invalidValues.Count > 0)
                    {
                        errors.Add($"{label} có mã không hợp lệ: \"{string.Join(",", invalidValues)}\"");
                    }
                }
                if (field.ElementName == PackageSessionConst.DOC_CODE && packageType == PackageConst.SIP_HOSO
                    && !Regex.IsMatch(value, $"^{Regex.Escape(packageCode ?? String.Empty)}\\.[0-9]{{{PackageSessionConst.DOC_ORDINAL_LENGTH}}}$"))
                {
                    errors.Add($"{label} phải là mã hồ sơ \"{packageCode}\" + \".\" + {PackageSessionConst.DOC_ORDINAL_LENGTH} chữ số: \"{value}\"");
                }
                if (field.ElementName == PackageSessionConst.DOC_CODE && packageType == PackageConst.SIP_TAILIEU
                    && !Regex.IsMatch(value, $"\\.[0-9]{{{PackageSessionConst.DOC_ORDINAL_LENGTH}}}$"))
                {
                    errors.Add($"{label} phải kết thúc bằng \".\" + {PackageSessionConst.DOC_ORDINAL_LENGTH} chữ số: \"{value}\"");
                }
            }
            return errors;
        }

        public static PackageVerifyPlan BuildVerifyPlan(PackageSession session, PackageVerifyConfig config, List<PackageDocument> documents,
            List<PackageExcelWorkbook> workbooks, string? userId, DateTime now)
        {
            var packageSchema = config.Schemas.First(s => s.MetadataType == PackageSchemaConst.METADATA_PACKAGE);
            var documentSchemas = config.Schemas.Where(s => s.MetadataType != PackageSchemaConst.METADATA_PACKAGE).ToList();
            var packageCodeElement = PackageSessionConst.PACKAGE_CODE_ELEMENTS.FirstOrDefault(e => packageSchema.Fields.Any(f => f.ElementName == e)) ?? String.Empty;
            var checks = documents.ToDictionary(d => d.Id, d => new PackageDocumentCheck(d));

            var excelByFolder = new Dictionary<string, PackageDocument?>(StringComparer.OrdinalIgnoreCase);
            foreach (var folderExcels in workbooks.Select(w => w.Excel).GroupBy(e => GetFolder(e.RelativePath), StringComparer.OrdinalIgnoreCase))
            {
                var isUnique = folderExcels.Count() == 1;
                excelByFolder[folderExcels.Key] = isUnique ? folderExcels.First() : null;
                foreach (var excel in folderExcels.Where(_ => !isUnique))
                {
                    checks[excel.Id].Errors.Add($"Thư mục \"{folderExcels.Key}\" có {folderExcels.Count()} file Excel, chỉ được 1");
                }
            }

            var excelContexts = new Dictionary<int, PackageExcelContext>();
            var reports = new List<PackageExcelReport>();
            foreach (var workbook in workbooks.Where(w => excelByFolder[GetFolder(w.Excel.RelativePath)] != null))
            {
                var excelErrors = checks[workbook.Excel.Id].Errors;
                var context = new PackageExcelContext(workbook.Excel, GetFolder(workbook.Excel.RelativePath));
                var sheetReports = new List<PackageSheetReport>();
                if (workbook.Sheets == null)
                {
                    excelErrors.Add("Không đọc được file Excel, chỉ nhận file .xlsx");
                }
                foreach (var schema in workbook.Sheets == null ? new List<PackageVerifySchema>() : config.Schemas)
                {
                    if (!workbook.Sheets!.TryGetValue(schema.Name, out var table))
                    {
                        excelErrors.Add($"Thiếu sheet \"{schema.Name}\"");
                        continue;
                    }
                    var match = MatchSheetColumns(schema, table.Headers);
                    sheetReports.Add(match.Report);
                    if (match.Report.MissingRequired.Count > 0)
                    {
                        excelErrors.Add($"Sheet \"{schema.Name}\" thiếu cột bắt buộc: {string.Join(", ", match.Report.MissingRequired)}");
                    }
                    context.Sheets[schema.MetadataType] = (match, table.Rows.Where(r => r.Cells.Any(c => SubmissionImportDomainService.NormalizeImportValue(c).Length > 0)).ToList());
                }
                reports.Add(new PackageExcelReport(workbook.Excel.Id, workbook.Excel.RelativePath, excelErrors, sheetReports));
                if (excelErrors.Count > 0)
                {
                    continue;
                }

                var (packageMatch, packageRows) = context.Sheets[PackageSchemaConst.METADATA_PACKAGE];
                if (session.PackageType == PackageConst.DIP && packageRows.Count != 1)
                {
                    excelErrors.Add($"Sheet \"{packageSchema.Name}\" của gói DIP phải có đúng 1 dòng, đang có {packageRows.Count}");
                    continue;
                }
                foreach (var row in packageRows)
                {
                    var packageCode = packageMatch.GetCell(row, packageCodeElement);
                    var dossier = PackageDossier.Create(session.Id, workbook.Excel.Id, packageCode.Length == 0 ? null : packageCode, row.SheetRowNumber, context.Folder, userId, now);
                    var dossierCheck = new PackageDossierCheck(dossier);
                    dossierCheck.Errors.AddRange(ValidateRow(session.PackageType, packageMatch, row, config.CodesByGroup, null).Select(e => $"Dòng {row.SheetRowNumber}: {e}"));
                    if (packageCode.Length == 0)
                    {
                        dossierCheck.Errors.Add($"Dòng {row.SheetRowNumber}: mã gói trống");
                    }
                    context.Dossiers.Add(dossierCheck);
                }
                foreach (var duplicates in context.Dossiers.Where(d => d.Dossier.PackageCode != null).GroupBy(d => d.Dossier.PackageCode!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                {
                    foreach (var dossierCheck in duplicates)
                    {
                        dossierCheck.Errors.Add($"Mã gói \"{duplicates.Key}\" trùng ở các dòng {string.Join(", ", duplicates.Select(d => d.Dossier.RowIndex))}");
                    }
                }

                foreach (var schema in documentSchemas)
                {
                    var (match, rows) = context.Sheets[schema.MetadataType];
                    foreach (var row in rows)
                    {
                        var docKey = ResolveDocKey(match, row).Normalize(NormalizationForm.FormC);
                        var (dossierCheck, dossierError) = FindDossier(context, packageSchema, match, row, docKey);
                        context.Rows.Add(new PackageDocumentRow(match, row, docKey, dossierCheck, dossierError));
                    }
                }
                excelContexts[workbook.Excel.Id] = context;
            }

            foreach (var check in checks.Values.Where(c => c.FileRole != PackageSessionConst.FILE_ROLE_EXCEL))
            {
                var folder = GetFolder(check.Document.RelativePath);
                PackageDocument? excel = null;
                var hasExcel = false;
                while (!hasExcel)
                {
                    hasExcel = excelByFolder.TryGetValue(folder, out excel);
                    if (folder.Length == 0)
                    {
                        break;
                    }
                    folder = GetFolder(folder);
                }
                if (!hasExcel)
                {
                    check.Errors.Add("Không tìm thấy file Excel metadata áp dụng cho thư mục này");
                    continue;
                }
                if (excel == null || !excelContexts.TryGetValue(excel.Id, out var context))
                {
                    check.Errors.Add($"File Excel áp dụng cho thư mục này bị lỗi{(excel == null ? String.Empty : $": {excel.RelativePath}")}");
                    continue;
                }

                var fileStem = Path.GetFileNameWithoutExtension(check.Document.FileName).Trim().Normalize(NormalizationForm.FormC);
                var dipDossier = session.PackageType == PackageConst.DIP ? context.Dossiers[0] : null;
                if (dipDossier != null && check.Document.Extension == PackageSessionConst.PDF_EXTENSION
                    && string.Equals(GetFolder(check.Document.RelativePath), context.Folder, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(fileStem, dipDossier.Dossier.PackageCode, StringComparison.OrdinalIgnoreCase))
                {
                    check.FileRole = PackageSessionConst.FILE_ROLE_DIP_AUTH;
                    check.Dossier = dipDossier.Dossier;
                    dipDossier.HasAuthFile = true;
                    if (check.Document.PdfAPart == null)
                    {
                        check.Warnings.Add("File xác thực không phải PDF/A");
                    }
                    continue;
                }

                var matchedRows = context.Rows.Where(r => string.Equals(r.DocKey, fileStem, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchedRows.Count == 0)
                {
                    check.Errors.Add($"Không có dòng metadata khớp tên file \"{fileStem}\"");
                    continue;
                }
                if (matchedRows.Count > 1)
                {
                    check.Errors.Add($"Mã \"{fileStem}\" khai ở nhiều dòng: {string.Join(", ", matchedRows.Select(r => $"sheet \"{r.Match.Schema.Name}\" dòng {r.Row.SheetRowNumber}"))}");
                    continue;
                }

                var documentRow = matchedRows[0];
                documentRow.HasFile = true;
                check.MetadataType = documentRow.Match.Schema.MetadataType;
                check.DocKey = documentRow.DocKey;
                if (documentRow.DossierCheck == null)
                {
                    check.Errors.Add(documentRow.DossierError!);
                    continue;
                }
                check.Dossier = documentRow.DossierCheck.Dossier;
                check.Errors.AddRange(ValidateRow(session.PackageType, documentRow.Match, documentRow.Row, config.CodesByGroup, check.Dossier.PackageCode)
                    .Select(e => $"Sheet \"{documentRow.Match.Schema.Name}\" dòng {documentRow.Row.SheetRowNumber}: {e}"));
                if (check.Document.Extension == PackageSessionConst.PDF_EXTENSION && !check.Document.IsSigned)
                {
                    check.Warnings.Add("PDF chưa ký số");
                }
                if (check.Document.Extension == PackageSessionConst.PDF_EXTENSION && check.Document.PdfAPart == null)
                {
                    check.Warnings.Add("PDF không phải PDF/A");
                }
            }

            var dataChecks = checks.Values.Where(c => c.FileRole == PackageSessionConst.FILE_ROLE_DATA && c.Dossier != null).ToList();
            foreach (var duplicates in dataChecks.GroupBy(c => (c.Dossier, c.DocKey!.ToUpperInvariant())).Where(g => g.Count() > 1))
            {
                foreach (var check in duplicates)
                {
                    check.Errors.Add($"Trùng tên file \"{check.DocKey}\" trong gói \"{check.Dossier!.PackageCode}\": {string.Join(", ", duplicates.Select(d => d.Document.RelativePath))}");
                }
            }

            foreach (var context in excelContexts.Values)
            {
                foreach (var documentRow in context.Rows.Where(r => !r.HasFile && r.DossierCheck != null))
                {
                    documentRow.DossierCheck!.Warnings.Add($"Sheet \"{documentRow.Match.Schema.Name}\" dòng {documentRow.Row.SheetRowNumber} (mã \"{documentRow.DocKey}\") chưa có file");
                }
                foreach (var dossierCheck in context.Dossiers.Where(d => session.PackageType == PackageConst.DIP && !d.HasAuthFile))
                {
                    dossierCheck.Errors.Add($"Thiếu file xác thực \"{dossierCheck.Dossier.PackageCode}{PackageSessionConst.PDF_EXTENSION}\" cùng thư mục với Excel");
                }
            }

            var documentChecks = checks.Values.ToList();
            return new PackageVerifyPlan(
                excelContexts.Values.SelectMany(c => c.Dossiers).ToList(),
                documentChecks,
                reports,
                documentChecks.Any(c => c.Dossier == null && c.Errors.Count > 0));
        }

        private static (PackageDossierCheck? DossierCheck, string? Error) FindDossier(PackageExcelContext context, PackageVerifySchema packageSchema, PackageSheetMatch match,
            SheetRow row, string docKey)
        {
            if (context.Dossiers.Count == 1)
            {
                return (context.Dossiers[0], null);
            }

            var notFoundError = $"Mã \"{docKey}\" không thuộc mã gói nào khai trong sheet \"{packageSchema.Name}\"";
            if (match.Schema.Fields.Any(f => f.ElementName == PackageSessionConst.ARC_FILE_CODE))
            {
                var arcFileCode = match.GetCell(row, PackageSessionConst.ARC_FILE_CODE);
                var dossier = context.Dossiers.FirstOrDefault(d => string.Equals(d.Dossier.PackageCode, arcFileCode, StringComparison.OrdinalIgnoreCase));
                return (dossier, dossier == null ? $"Mã \"{arcFileCode}\" không thuộc mã gói nào khai trong sheet \"{packageSchema.Name}\"" : null);
            }

            var longestDossier = context.Dossiers
                .Where(d => d.Dossier.PackageCode != null
                    && (string.Equals(docKey, d.Dossier.PackageCode, StringComparison.OrdinalIgnoreCase)
                        || docKey.StartsWith($"{d.Dossier.PackageCode}.", StringComparison.OrdinalIgnoreCase)))
                .MaxBy(d => d.Dossier.PackageCode!.Length);
            return (longestDossier, longestDossier == null ? notFoundError : null);
        }
    }

    public record PackageUploadCheck(string RelativePath, string FileName, string Extension, string? FileRole, string? Reason, bool IsSkipped)
    {
        public bool IsAccepted => FileRole != null;
    }

    public record PackageVerifySchema(int MetadataType, string Name, List<PackageSchemaField> Fields);

    public record PackageVerifyConfig(List<PackageVerifySchema> Schemas, Dictionary<string, HashSet<string>> CodesByGroup);

    public record PackageExcelWorkbook(PackageDocument Excel, Dictionary<string, SheetTable>? Sheets);

    public record PackageMatchedColumn(string Header, string ElementName, string DisplayName, double Score);

    public record PackageSheetReport(string SheetName, int MetadataType, List<PackageMatchedColumn> Matched, List<string> Missing, List<string> MissingRequired, List<string> Unused);

    public record PackageExcelReport(int ExcelDocumentId, string RelativePath, List<string> Errors, List<PackageSheetReport> Sheets);

    public record PackageSheetMatch(PackageVerifySchema Schema, Dictionary<string, int> Columns, PackageSheetReport Report)
    {
        public string GetCell(SheetRow row, string elementName)
        {
            return Columns.TryGetValue(elementName, out var columnIndex)
                ? SubmissionImportDomainService.GetImportCell(row, columnIndex)
                : String.Empty;
        }
    }

    public record PackageVerifyPlan(List<PackageDossierCheck> Dossiers, List<PackageDocumentCheck> Documents, List<PackageExcelReport> ExcelReports, bool HasErrorOutsideDossier);

    public class PackageDossierCheck(PackageDossier dossier)
    {
        public PackageDossier Dossier { get; } = dossier;
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool HasAuthFile { get; set; }
    }

    public class PackageDocumentCheck(PackageDocument document)
    {
        public PackageDocument Document { get; } = document;
        public string FileRole { get; set; } = document.FileRole;
        public PackageDossier? Dossier { get; set; }
        public int? MetadataType { get; set; }
        public string? DocKey { get; set; }
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    public class PackageDocumentRow(PackageSheetMatch match, SheetRow row, string docKey, PackageDossierCheck? dossierCheck, string? dossierError)
    {
        public PackageSheetMatch Match { get; } = match;
        public SheetRow Row { get; } = row;
        public string DocKey { get; } = docKey;
        public PackageDossierCheck? DossierCheck { get; } = dossierCheck;
        public string? DossierError { get; } = dossierError;
        public bool HasFile { get; set; }
    }

    public class PackageExcelContext(PackageDocument excel, string folder)
    {
        public PackageDocument Excel { get; } = excel;
        public string Folder { get; } = folder;
        public Dictionary<int, (PackageSheetMatch Match, List<SheetRow> Rows)> Sheets { get; } = new();
        public List<PackageDossierCheck> Dossiers { get; } = new();
        public List<PackageDocumentRow> Rows { get; } = new();
    }
}
