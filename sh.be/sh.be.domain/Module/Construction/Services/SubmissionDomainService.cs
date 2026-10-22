using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.Services
{
    public class SubmissionDomainService
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IDocumentTypeRepository _documentTypeRepository;

        public SubmissionDomainService(ISubmissionRepository submissionRepository, IProjectRepository projectRepository,
            IDocumentTypeRepository documentTypeRepository)
        {
            _submissionRepository = submissionRepository;
            _projectRepository = projectRepository;
            _documentTypeRepository = documentTypeRepository;
        }

        public async Task<Project> EnsureImportableProjectAsync(int projectId)
        {
            var project = await _projectRepository.FindByIdAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            project.EnsureImportable();
            return project;
        }

        public async Task<Project> EnsureProjectExistsAsync(int projectId)
        {
            return await _projectRepository.FindByIdAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
        }

        public async Task<Submission> EnsureSubmissionExistsAsync(int submissionId)
        {
            return await _submissionRepository.FindByIdAsync(submissionId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        public async Task<List<Submission>> EnsureSubmissionsDeletableAsync(int projectId, int documentTypeId, IReadOnlyCollection<int> ids, bool isSuperAdmin)
        {
            var distinctIds = ids.Distinct().ToList();
            if (distinctIds.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chưa chọn hồ sơ");
            }
            if (distinctIds.Count > SubmissionConst.DELETE_MAX_ITEMS)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Tối đa {SubmissionConst.DELETE_MAX_ITEMS} hồ sơ mỗi lần");
            }

            var submissions = await _submissionRepository.GetByIdsAsync(distinctIds);
            var missingIds = distinctIds.Except(submissions.Select(submission => submission.Id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound, $"Hồ sơ không tồn tại: Id = {string.Join(", ", missingIds)}");
            }
            foreach (var submission in submissions)
            {
                submission.EnsureBelongsTo(projectId, documentTypeId);
            }

            var checkedIds = submissions.Where(submission => submission.IsChecked()).Select(submission => submission.Id).ToList();
            if (!isSuperAdmin && checkedIds.Count > 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionCheckedNotDeletable,
                    $"Không được xoá hồ sơ đã kiểm tra: Id = {string.Join(", ", checkedIds)}");
            }
            return submissions;
        }

        public async Task<Project> EnsureEditableProjectAsync(int projectId, bool isSuperAdmin)
        {
            var project = await EnsureProjectExistsAsync(projectId);
            project.EnsureEditable(isSuperAdmin);
            return project;
        }

        public async Task EnsureReadableDocumentTypeAsync(int projectId, int documentTypeId)
        {
            if (await _projectRepository.ExistsActiveDocumentTypeAsync(projectId, documentTypeId))
            {
                return;
            }
            await EnsureProjectExistsAsync(projectId);
            await EnsureDocumentTypeInProjectAsync(projectId, documentTypeId);
        }

        public async Task EnsureDocumentTypeInProjectAsync(int projectId, int documentTypeId)
        {
            if (await _documentTypeRepository.FindByIdAsync(documentTypeId) == null)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotFound);
            }
            if (!await _projectRepository.ExistsDocumentTypeAsync(projectId, documentTypeId))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotInProject);
            }
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetImportMappingsAsync(int projectId, int documentTypeId)
        {
            return _projectRepository.GetDocumentFieldsAsync(projectId, documentTypeId);
        }

        public async Task<ProjectDocumentTypeDocumentField> EnsureDocumentFieldInProjectAsync(int projectId, int documentTypeId, int documentFieldId)
        {
            var mappings = await _projectRepository.GetDocumentFieldsAsync(projectId, documentTypeId);
            return mappings.FirstOrDefault(mapping => mapping.DocumentFieldId == documentFieldId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentFieldNotInProject);
        }

        public async Task EnsureFileUrlExistsAsync(string fileUrl)
        {
            if (!await _submissionRepository.ExistsByFileUrlAsync(fileUrl))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            }
        }

        public Task LockImportAsync(int projectId, int documentTypeId)
        {
            return _submissionRepository.LockImportAsync(projectId, documentTypeId);
        }
    }
}
