using System.Text;

namespace sh.be.domain.Module.Construction.Submissions.Services.Import
{
    public static class SheetCellAddressPolicy
    {
        private const int AlphabetLength = 26;

        public static string GetColumnLetter(int columnIndex)
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

        public static string BuildAddress(int columnIndex, int sheetRowNumber)
        {
            return $"{GetColumnLetter(columnIndex)}{sheetRowNumber}";
        }
    }
}
