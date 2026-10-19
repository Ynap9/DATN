using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ShDbContext _shDbContext;

        public ProjectRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<Project?> FindByIdAsync(int id)
        {
            return _shDbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null)
        {
            return _shDbContext.Projects.AnyAsync(p => p.Name == name && p.Id != excludedId);
        }

        public void Add(Project project)
        {
            _shDbContext.Projects.Add(project);
        }

        public void Update(Project project)
        {
            _shDbContext.Projects.Update(project);
        }

        public Task<List<ProjectDocumentType>> GetDocumentTypesAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypes
                .Where(pd => pd.ProjectId == projectId)
                .ToListAsync();
        }

        public Task<List<int>> GetDocumentTypeIdsAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypes
                .Where(pd => pd.ProjectId == projectId)
                .Select(pd => pd.DocumentTypeId)
                .ToListAsync();
        }

        public Task<bool> ExistsDocumentTypeAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.ProjectId == projectId && pd.DocumentTypeId == documentTypeId);
        }

        public Task<bool> ExistsActiveDocumentTypeAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.ProjectId == projectId && pd.DocumentTypeId == documentTypeId
                && _shDbContext.Projects.Any(p => p.Id == projectId)
                && _shDbContext.DocumentTypes.Any(d => d.Id == documentTypeId));
        }

        public void AddDocumentTypes(IEnumerable<ProjectDocumentType> projectDocumentTypes)
        {
            _shDbContext.ProjectDocumentTypes.AddRange(projectDocumentTypes);
        }

        public Task DeleteDocumentTypesAsync(int projectId, IReadOnlyCollection<int> documentTypeIds)
        {
            return _shDbContext.ProjectDocumentTypes
                .IgnoreQueryFilters()
                .Where(pd => pd.ProjectId == projectId && documentTypeIds.Contains(pd.DocumentTypeId))
                .ExecuteDeleteAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .Where(pf => pf.ProjectId == projectId)
                .ToListAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .AsNoTracking()
                .Where(pf => pf.ProjectId == projectId && pf.DocumentTypeId == documentTypeId)
                .ToListAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsByDocumentFieldIdsAsync(IReadOnlyCollection<int> documentFieldIds)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .Where(pf => documentFieldIds.Contains(pf.DocumentFieldId))
                .ToListAsync();
        }

        public void AddDocumentFields(IEnumerable<ProjectDocumentTypeDocumentField> projectDocumentFields)
        {
            _shDbContext.ProjectDocumentTypeDocumentFields.AddRange(projectDocumentFields);
        }

        public Task DeleteDocumentFieldsAsync(int projectId, IReadOnlyCollection<int> documentTypeIds)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .IgnoreQueryFilters()
                .Where(pf => pf.ProjectId == projectId && documentTypeIds.Contains(pf.DocumentTypeId))
                .ExecuteDeleteAsync();
        }
    }
}
