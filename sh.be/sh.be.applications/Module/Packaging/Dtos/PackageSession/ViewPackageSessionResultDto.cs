namespace sh.be.applications.Module.Packaging.Dtos.PackageSession
{
    public class ViewPackageSessionResultDto
    {
        public int Id { get; set; }
        public int PackageType { get; set; }
        public string MetadataTypes { get; set; } = String.Empty;
        public string? RootFolderName { get; set; }
        public string Status { get; set; } = String.Empty;
        public string? FailReason { get; set; }
        public string? FondsCode { get; set; }
        public string? ArchiveCode { get; set; }
        public string? ArchiveName { get; set; }
        public List<GetListPackageDossierDto> Dossiers { get; set; } = new();
        public List<ViewPackageExcelReportDto> Excels { get; set; } = new();
    }

    public class GetListPackageDossierDto
    {
        public int Id { get; set; }
        public int ExcelDocumentId { get; set; }
        public string? PackageCode { get; set; }
        public int RowIndex { get; set; }
        public string FolderName { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string? ErrorSummary { get; set; }
        public int DocumentCount { get; set; }
    }

    public class ViewPackageExcelReportDto
    {
        public int ExcelDocumentId { get; set; }
        public string RelativePath { get; set; } = String.Empty;
        public List<string> Errors { get; set; } = new();
        public List<ViewPackageSheetReportDto> Sheets { get; set; } = new();
    }

    public class ViewPackageSheetReportDto
    {
        public string SheetName { get; set; } = String.Empty;
        public int MetadataType { get; set; }
        public List<ViewPackageMatchedColumnDto> Matched { get; set; } = new();
        public List<string> Missing { get; set; } = new();
        public List<string> MissingRequired { get; set; } = new();
        public List<string> Unused { get; set; } = new();
    }

    public class ViewPackageMatchedColumnDto
    {
        public string Header { get; set; } = String.Empty;
        public string ElementName { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
        public double Score { get; set; }
    }
}
