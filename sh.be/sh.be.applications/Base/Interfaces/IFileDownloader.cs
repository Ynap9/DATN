using sh.be.applications.Base.Dtos.File;

namespace sh.be.applications.Base.Interfaces
{
    public interface IFileDownloader
    {
        Task<FileStreamDto> OpenPdfAsync(string fileUrl, CancellationToken cancellationToken);
    }
}
