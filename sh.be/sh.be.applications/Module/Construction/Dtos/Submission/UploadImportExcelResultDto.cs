using sh.be.applications.Base.Dtos.DataReader;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class UploadImportExcelResultDto
    {
        public string FileKey { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public List<SheetHeaderDto> Sheets { get; set; } = new();
    }
}
