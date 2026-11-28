using sh.be.domain.Module.Packaging.Entities;

namespace sh.be.domain.Module.Packaging.Repositories
{
    public interface IPackageSessionRepository
    {
        Task<PackageSession?> FindByIdAsync(int id);
        Task<string?> GetStatusAsync(int id);
        void Add(PackageSession session);
        void Update(PackageSession session);
        Task<int> CountDocumentsByRoleAsync(int sessionId, string fileRole);
        Task<List<PackageDocument>> GetDocumentsAsync(int sessionId);
        void AddDocuments(List<PackageDocument> documents);
        Task<List<PackageDossier>> GetDossiersAsync(int sessionId);
        void AddDossiers(List<PackageDossier> dossiers);
        Task<List<PackageSession>> GetExpiredAsync(DateTime now);
    }
}
