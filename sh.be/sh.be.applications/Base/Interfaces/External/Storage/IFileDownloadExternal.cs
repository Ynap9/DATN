using sh.be.applications.Base.Dtos.Storage;

namespace sh.be.applications.Base.Interfaces.External.Storage
{
    public interface IFileDownloadExternal
    {
        Task<FileStreamDto> OpenPdfAsync(string fileUrl, CancellationToken cancellationToken);
    }
}
