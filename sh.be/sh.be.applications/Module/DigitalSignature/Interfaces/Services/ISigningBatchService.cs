using sh.be.applications.Base.Dtos.Signing;
using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Services
{
    public interface ISigningBatchService
    {
        Task<FindSigningBatchByIdDto?> FindRunning();
        Task<List<GetListSigningBatchFileDto>> GetFiles(int id);
        Task<FindSigningBatchByIdDto> Create(CreateSigningBatchDto dto);
        Task<FindSigningBatchByIdDto> AddFiles(int id, AddFilesSigningBatchDto dto);
        Task<FindSigningBatchByIdDto> RemoveFile(int id, int fileId);
        Task<FindSigningBatchByIdDto> OpenSession(int id, OpenSessionSigningBatchDto dto);
        Task CloseSession(int id);
        Task<FindSigningBatchByIdDto> Start(int id);
        Task<FindSigningBatchByIdDto> Pause(int id);
        Task<FindSigningBatchByIdDto> Cancel(int id);
        Task<SigningBatchProgressDto> GetProgress(int id);
        Task<List<SignatureRequestDto>> TakeRequests(int id, CancellationToken cancellationToken);
        Task<List<SignatureRequestDto>> SubmitSignatures(int id, List<SignatureResultDto> results, CancellationToken cancellationToken);
        Task<string> GetSignedFileUrl(int id, int fileId);
    }
}
