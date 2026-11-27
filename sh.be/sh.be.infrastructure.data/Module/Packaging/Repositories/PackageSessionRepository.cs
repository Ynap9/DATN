using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Packaging.Entities;
using sh.be.domain.Module.Packaging.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Packaging.Repositories
{
    public class PackageSessionRepository : IPackageSessionRepository
    {
        private readonly ShDbContext _shDbContext;

        public PackageSessionRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<PackageSession?> FindByIdAsync(int id)
        {
            return _shDbContext.PackageSessions.FirstOrDefaultAsync(ps => ps.Id == id);
        }

        public Task<string?> GetStatusAsync(int id)
        {
            return _shDbContext.PackageSessions
                .AsNoTracking()
                .Where(ps => ps.Id == id)
                .Select(ps => ps.Status)
                .FirstOrDefaultAsync();
        }

        public void Add(PackageSession session)
        {
            _shDbContext.PackageSessions.Add(session);
        }

        public void Update(PackageSession session)
        {
            _shDbContext.PackageSessions.Update(session);
        }

        public Task<int> CountAllDocumentsAsync(int sessionId)
        {
            return _shDbContext.PackageDocuments
                .IgnoreQueryFilters()
                .CountAsync(pd => pd.SessionId == sessionId);
        }

        public Task<int> CountDocumentsByRoleAsync(int sessionId, string fileRole)
        {
            return _shDbContext.PackageDocuments.CountAsync(pd => pd.SessionId == sessionId && pd.FileRole == fileRole);
        }

        public Task<List<PackageDocument>> GetDocumentsAsync(int sessionId)
        {
            return _shDbContext.PackageDocuments
                .Where(pd => pd.SessionId == sessionId)
                .OrderBy(pd => pd.Id)
                .ToListAsync();
        }

        public void AddDocuments(List<PackageDocument> documents)
        {
            _shDbContext.PackageDocuments.AddRange(documents);
        }

        public Task<List<PackageDossier>> GetDossiersAsync(int sessionId)
        {
            return _shDbContext.PackageDossiers
                .Where(pd => pd.SessionId == sessionId)
                .ToListAsync();
        }

        public void AddDossiers(List<PackageDossier> dossiers)
        {
            _shDbContext.PackageDossiers.AddRange(dossiers);
        }

        public Task<List<PackageSession>> GetExpiredAsync(DateTime now)
        {
            return _shDbContext.PackageSessions
                .Where(ps => ps.ExpiredDate < now)
                .ToListAsync();
        }
    }
}
