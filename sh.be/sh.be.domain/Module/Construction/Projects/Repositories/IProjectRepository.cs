using sh.be.domain.Module.Construction.Projects.Entities;

namespace sh.be.domain.Module.Construction.Projects.Repositories
{
    public interface IProjectRepository
    {
        Task<Project?> FindByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludedId = null);
        void Add(Project project);
        void Update(Project project);
        Task<List<ProjectDocumentType>> GetDocumentTypesAsync(int projectId);
        Task<List<int>> GetDocumentTypeIdsAsync(int projectId);
        Task<bool> ExistsDocumentTypeAsync(int projectId, int documentTypeId);
        Task<bool> ExistsActiveDocumentTypeAsync(int projectId, int documentTypeId);
        void AddDocumentTypes(IEnumerable<ProjectDocumentType> projectDocumentTypes);
        Task DeleteDocumentTypesAsync(int projectId, IReadOnlyCollection<int> documentTypeIds);
        Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId);
        Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId, int documentTypeId);
        Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsByDocumentFieldIdsAsync(IReadOnlyCollection<int> documentFieldIds);
        void AddDocumentFields(IEnumerable<ProjectDocumentTypeDocumentField> projectDocumentFields);
        Task DeleteDocumentFieldsAsync(int projectId, IReadOnlyCollection<int> documentTypeIds);
    }
}
