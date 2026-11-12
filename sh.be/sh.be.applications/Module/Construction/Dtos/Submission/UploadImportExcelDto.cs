using Microsoft.AspNetCore.Http;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class UploadImportExcelDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
