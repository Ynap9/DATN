using sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch;

namespace sh.be.applications.Module.DigitalSignature.Interfaces.Queries
{
    public interface ISigningBatchQuery
    {
        Task<FindSigningBatchByIdDto?> FindById(int id);
        Task<FindSigningBatchByIdDto?> FindRunning(string createdBy);
        Task<List<GetListSigningBatchFileDto>> GetFiles(int signingBatchId);
    }
}
