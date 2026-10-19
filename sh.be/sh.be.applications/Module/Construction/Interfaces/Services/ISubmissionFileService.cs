using sh.be.applications.Base.Dtos.File;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface ISubmissionFileService
    {
        Task<FileStreamDto> OpenPdf(string fileUrl, CancellationToken cancellationToken);
    }
}
