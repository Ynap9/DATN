using System.Text.RegularExpressions;

namespace sh.be.infrastructure.external.DataReader.Base
{
    public static class ExcelNaming
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string ExcelExtension = ".xlsx";
        private const string DefaultName = "Sheet1";
        private const int MaxSheetNameLength = 31;
        private static readonly Regex InvalidSheetNameCharsRegex = new(@"[\[\]:*?/\\]", RegexOptions.Compiled);

        public static string ToSheetName(string name)
        {
            var sheetName = InvalidSheetNameCharsRegex.Replace(name, "_").Trim().Trim('\'');
            if (sheetName.Length > MaxSheetNameLength)
            {
                sheetName = sheetName[..MaxSheetNameLength];
            }
            return string.IsNullOrWhiteSpace(sheetName) ? DefaultName : sheetName;
        }

        public static string ToFileName(string name, string suffix = "")
        {
            var fileName = string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Trim();
            return (string.IsNullOrWhiteSpace(fileName) ? DefaultName : fileName) + suffix + ExcelExtension;
        }
    }
}
