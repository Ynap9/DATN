using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services.Deadline;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Projects.Services
{
    public class ProjectConfigDomainService
    {
        private readonly IProjectConfigRepository _projectConfigRepository;

        public ProjectConfigDomainService(IProjectConfigRepository projectConfigRepository)
        {
            _projectConfigRepository = projectConfigRepository;
        }

        public async Task EnsureUniqueNameAsync(string name, int? excludedId = null)
        {
            if (await _projectConfigRepository.ExistsByNameAsync(name.Trim(), excludedId))
            {
                throw new UserFriendlyException(ErrorCodes.Found, "Tên cấu hình đã tồn tại");
            }
        }

        public async Task<IReadOnlyDictionary<string, string>> GetRequiredValuesAsync(params string[] names)
        {
            var projectConfigs = await _projectConfigRepository.FindByNamesAsync(names);
            var values = projectConfigs
                .Where(projectConfig => !string.IsNullOrWhiteSpace(projectConfig.Value))
                .ToDictionary(projectConfig => projectConfig.Name, projectConfig => projectConfig.Value);

            var missingNames = names.Where(name => !values.ContainsKey(name)).ToList();
            if (missingNames.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectConfigNotFound,
                    $"Thiếu cấu hình dự án: {string.Join(", ", missingNames)}");
            }
            return values;
        }

        public async Task<ProjectDeadlineSettings> GetDeadlineSettingsAsync()
        {
            var projectConfigs = await _projectConfigRepository.FindByNamesAsync(
                [ProjectConfigConst.DEADLINE_WARNING_DAYS, ProjectConfigConst.DEADLINE_MAIL_ENABLED]);
            var configValues = projectConfigs.ToDictionary(projectConfig => projectConfig.Name, projectConfig => projectConfig.Value.Trim());
            return ProjectDeadlinePolicy.ResolveSettings(configValues);
        }

        public async Task<ProjectConfig> SaveValueAsync(string name, string value, string? modifiedBy, DateTime modifiedDate)
        {
            var projectConfig = await _projectConfigRepository.FindByNameAsync(name);
            if (projectConfig == null)
            {
                projectConfig = ProjectConfig.Create(name, value, null, modifiedBy, modifiedDate);
                _projectConfigRepository.Add(projectConfig);
                return projectConfig;
            }

            projectConfig.ChangeValue(value, modifiedBy, modifiedDate);
            _projectConfigRepository.Update(projectConfig);
            return projectConfig;
        }
    }
}
