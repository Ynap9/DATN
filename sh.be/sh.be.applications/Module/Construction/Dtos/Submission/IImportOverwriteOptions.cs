namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public interface IImportOverwriteOptions
    {
        string ColumnImport { get; }
        int DocumentFieldId { get; }
        bool OverwriteChecked { get; }
    }
}
