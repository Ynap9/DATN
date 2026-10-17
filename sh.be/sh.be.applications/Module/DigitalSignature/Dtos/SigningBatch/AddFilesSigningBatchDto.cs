using Microsoft.AspNetCore.Http;

namespace sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch
{
    public class AddFilesSigningBatchDto
    {
        public List<IFormFile> Files { get; set; } = new();
    }
}
