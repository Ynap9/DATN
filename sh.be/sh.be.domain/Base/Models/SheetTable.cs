namespace sh.be.domain.Base.Models
{
    public record SheetTable(IReadOnlyList<string> Headers, IReadOnlyList<SheetRow> Rows);
}
