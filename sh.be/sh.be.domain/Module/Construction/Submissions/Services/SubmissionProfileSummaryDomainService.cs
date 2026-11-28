using sh.be.shared.Constants.Construction;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace sh.be.domain.Module.Construction.Submissions.Services
{
    // Gộp văn bản thành bảng theo hồ sơ (Hộp + Hồ sơ); hàm thuần, không gọi DB / Google để module đóng gói M2 dùng lại
    public static class SubmissionProfileSummaryDomainService
    {
        // (mẫu, nhóm năm, nhóm tháng, nhóm ngày) trên chuỗi đã bỏ dấu, chữ thường: "2024-04-26", "26/04/2024" / "5-11-2024", "17 tháng 05 năm 2024"
        private static readonly (Regex Pattern, int Year, int Month, int Day)[] DatePatterns =
        [
            (new Regex(@"(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})", RegexOptions.Compiled), 1, 2, 3),
            (new Regex(@"(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})", RegexOptions.Compiled), 3, 2, 1),
            (new Regex(@"(\d{1,2})\s*thang\s*(\d{1,2})\s*nam\s*(\d{4})", RegexOptions.Compiled), 3, 2, 1),
        ];
        private static readonly Regex NumberToken = new(@"\d[\d.,]*", RegexOptions.Compiled);
        private static readonly Regex ThousandsDots = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);
        private static readonly Regex NaturalChunks = new(@"\d+|\D+", RegexOptions.Compiled);

        // Mỗi hồ sơ một dòng, sắp tự nhiên theo Hộp rồi Hồ sơ; văn bản trống cả Hộp lẫn Hồ sơ không thuộc hồ sơ nào nên bỏ qua.
        // fieldNames = tên mọi trường của metadata (quyết định cột nào có, trường nào là trường ngày); cột lấy từ trường chỉ có khi metadata có trường trùng tên
        public static ProfileSummaryTable BuildProfileSummary(List<string> fieldNames, List<ProfileSummarySubmission> submissions)
        {
            var normalizedFieldNames = fieldNames.Select(NormalizeName).ToHashSet();
            var dateFieldNames = normalizedFieldNames.Where(name => ProfileSummaryConst.DATE_FIELD_KEYWORDS.Any(name.Contains)).ToHashSet();
            var pageCountFieldName = NormalizeName(ProfileSummaryConst.PAGE_COUNT_FIELD_NAME);
            var fieldColumns = ProfileSummaryConst.FIELD_COLUMNS
                .Select(column => (Column: column, Names: column.AliasNames.Append(column.Header).Select(NormalizeName).ToHashSet()))
                .Where(item => normalizedFieldNames.Overlaps(item.Names))
                .ToList();

            var naturalComparer = Comparer<string>.Create(CompareNatural);
            var profiles = submissions
                .Where(submission => submission.Box.Trim().Length > 0 || submission.ProfileRange.Trim().Length > 0)
                .GroupBy(submission => (Box: submission.Box.Trim().ToLowerInvariant(), Profile: submission.ProfileRange.Trim().ToLowerInvariant()))
                .OrderBy(profile => profile.First().Box.Trim(), naturalComparer)
                .ThenBy(profile => profile.First().ProfileRange.Trim(), naturalComparer)
                .ToList();

            var rows = new List<List<string>>();
            foreach (var profile in profiles)
            {
                var values = profile
                    .SelectMany(submission => submission.Values)
                    .Select(value => (Name: NormalizeName(value.FieldName), Value: value.Value.Trim()))
                    .Where(value => value.Value.Length > 0)
                    .ToList();
                var dates = values
                    .Where(value => dateFieldNames.Contains(value.Name))
                    .Select(value => ParseLooseDate(value.Value))
                    .OfType<DateTime>()
                    .ToList();
                // Số trang lấy từ PageCount, không văn bản nào có thì cộng trường tên "Số trang"
                var pageCount = profile.Sum(submission => submission.PageCount is > 0 ? submission.PageCount.Value : 0);
                var pageCountText = pageCount > 0
                    ? pageCount.ToString(CultureInfo.InvariantCulture)
                    : MergeValues(ProfileSummaryConst.MERGE_SUM, values.Where(value => value.Name == pageCountFieldName).Select(value => value.Value).ToList());

                var first = profile.First();
                var row = new List<string>
                {
                    first.Box.Trim(),
                    first.ProfileRange.Trim(),
                    dates.Count > 0 ? dates.Min().ToString(ProfileSummaryConst.DATE_FORMAT, CultureInfo.InvariantCulture) : String.Empty,
                    dates.Count > 0 ? dates.Max().ToString(ProfileSummaryConst.DATE_FORMAT, CultureInfo.InvariantCulture) : String.Empty,
                    profile.Count().ToString(CultureInfo.InvariantCulture),
                    pageCountText,
                };
                row.AddRange(fieldColumns.Select(item => MergeValues(item.Column.MergeMethod,
                    values.Where(value => item.Names.Contains(value.Name)).Select(value => value.Value).ToList())));
                rows.Add(row);
            }

            List<string> headers = [.. ProfileSummaryConst.FIXED_HEADERS, .. fieldColumns.Select(item => item.Column.Header)];
            List<int> numberColumnIndexes =
            [
                .. ProfileSummaryConst.NUMBER_FIXED_HEADERS.Select(header => ProfileSummaryConst.FIXED_HEADERS.IndexOf(header)),
                .. Enumerable.Range(0, fieldColumns.Count)
                    .Where(index => fieldColumns[index].Column.MergeMethod == ProfileSummaryConst.MERGE_SUM)
                    .Select(index => ProfileSummaryConst.FIXED_HEADERS.Count + index),
            ];
            return new ProfileSummaryTable(headers, rows, numberColumnIndexes);
        }

        // Đọc một ngày trong chuỗi (kể cả khi nằm giữa chữ: "Hà Nội, ngày 12 tháng 11 năm 2024"); năm ngoài 1900–2100 hoặc ngày không có thật thì không đọc được
        public static DateTime? ParseLooseDate(string text)
        {
            var normalized = RemoveDiacritics(text);
            foreach (var (pattern, yearGroup, monthGroup, dayGroup) in DatePatterns)
            {
                var match = pattern.Match(normalized);
                if (!match.Success)
                {
                    continue;
                }

                var year = int.Parse(match.Groups[yearGroup].Value, CultureInfo.InvariantCulture);
                var month = int.Parse(match.Groups[monthGroup].Value, CultureInfo.InvariantCulture);
                var day = int.Parse(match.Groups[dayGroup].Value, CultureInfo.InvariantCulture);
                if (year < ProfileSummaryConst.DATE_MIN_YEAR || year > ProfileSummaryConst.DATE_MAX_YEAR || month < 1 || month > 12
                    || day < 1 || day > DateTime.DaysInMonth(year, month))
                {
                    return null;
                }
                return new DateTime(year, month, day);
            }
            return null;
        }

        // Đọc số đầu tiên trong chuỗi ("1.234" = 1234, "12,5" = 12,5, "12 trang" = 12); dấu cuối cùng là dấu thập phân khi có cả "," và "."
        public static decimal? ParseLooseNumber(string text)
        {
            var match = NumberToken.Match(text);
            if (!match.Success)
            {
                return null;
            }

            var token = match.Value.TrimEnd('.', ',');
            var lastComma = token.LastIndexOf(',');
            var lastDot = token.LastIndexOf('.');
            var decimalIndex = lastComma >= 0 && lastDot >= 0 ? Math.Max(lastComma, lastDot)
                : lastComma >= 0 ? lastComma
                : lastDot >= 0 && !ThousandsDots.IsMatch(token) ? lastDot
                : -1;
            var normalized = decimalIndex < 0
                ? token.Replace(".", "").Replace(",", "")
                : $"{token[..decimalIndex].Replace(".", "").Replace(",", "")}.{token[(decimalIndex + 1)..]}";
            return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) ? number : null;
        }

        // "common" = giá trị gặp nhiều nhất (bằng nhau lấy giá trị gặp trước); "sum" = cộng số đọc được; "join" = tách theo , và ;, bỏ trùng không phân biệt hoa thường, nối "; "
        private static string MergeValues(string mergeMethod, List<string> values)
        {
            switch (mergeMethod)
            {
                case ProfileSummaryConst.MERGE_SUM:
                    var numbers = values.Select(ParseLooseNumber).OfType<decimal>().ToList();
                    return numbers.Count == 0 ? String.Empty : numbers.Sum().ToString("0.##", CultureInfo.InvariantCulture);
                case ProfileSummaryConst.MERGE_JOIN:
                    return string.Join(ProfileSummaryConst.JOIN_SEPARATOR, values
                        .SelectMany(value => value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .DistinctBy(value => value, StringComparer.OrdinalIgnoreCase));
                default:
                    return values.GroupBy(value => value).OrderByDescending(group => group.Count()).Select(group => group.Key).FirstOrDefault() ?? String.Empty;
            }
        }

        // So khớp tên trường / cột: bỏ dấu, chữ thường, ký tự khác chữ và số thành khoảng trắng rồi gộp khoảng trắng
        private static string NormalizeName(string name)
        {
            var letters = RemoveDiacritics(name).Select(character => char.IsLetterOrDigit(character) ? character : ' ').ToArray();
            return string.Join(' ', new string(letters).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string RemoveDiacritics(string text)
        {
            var decomposed = text.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }
            return builder.ToString();
        }

        // Sắp tự nhiên: cụm số so theo giá trị ("2" trước "10"), cụm chữ so không phân biệt hoa thường; chuỗi trống đứng đầu
        private static int CompareNatural(string left, string right)
        {
            var leftChunks = NaturalChunks.Matches(left).Select(match => match.Value).ToList();
            var rightChunks = NaturalChunks.Matches(right).Select(match => match.Value).ToList();
            for (var index = 0; index < Math.Min(leftChunks.Count, rightChunks.Count); index++)
            {
                var leftChunk = leftChunks[index];
                var rightChunk = rightChunks[index];
                int result;
                if (char.IsDigit(leftChunk[0]) && char.IsDigit(rightChunk[0]))
                {
                    var leftDigits = leftChunk.TrimStart('0');
                    var rightDigits = rightChunk.TrimStart('0');
                    result = leftDigits.Length != rightDigits.Length ? leftDigits.Length.CompareTo(rightDigits.Length) : string.CompareOrdinal(leftDigits, rightDigits);
                }
                else
                {
                    result = string.Compare(leftChunk, rightChunk, StringComparison.OrdinalIgnoreCase);
                }
                if (result != 0)
                {
                    return result;
                }
            }
            return leftChunks.Count.CompareTo(rightChunks.Count);
        }
    }

    public record ProfileSummaryValue(string FieldName, string Value);

    public record ProfileSummarySubmission(string Box, string ProfileRange, int? PageCount, List<ProfileSummaryValue> Values);

    public record ProfileSummaryTable(List<string> Headers, List<List<string>> Rows, List<int> NumberColumnIndexes);
}
