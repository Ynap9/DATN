namespace sh.be.domain.Base.Models
{
    public record SheetRow(int SheetRowNumber, IReadOnlyList<string> Cells)
    {
        public string GetCell(int columnIndex)
        {
            return columnIndex >= 0 && columnIndex < Cells.Count ? Cells[columnIndex] : String.Empty;
        }
    }
}
