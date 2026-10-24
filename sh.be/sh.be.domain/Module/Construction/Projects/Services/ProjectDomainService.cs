using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Projects.Services
{
    public class ProjectDomainService
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectDomainService(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task EnsureUniqueNameAsync(string name, int? excludedId = null)
        {
            if (await _projectRepository.ExistsByNameAsync(name, excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên dự án đã tồn tại");
            }
        }
    }
}
