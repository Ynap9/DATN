using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.DocumentFields.Repositories;
using sh.be.domain.Module.Construction.DocumentTypes.Services;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class ProjectService : BaseService, IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IDocumentFieldRepository _documentFieldRepository;
        private readonly IProjectQuery _projectQuery;
        private readonly ProjectDomainService _projectDomainService;
        private readonly DocumentTypeDomainService _documentTypeDomainService;
        private readonly ProjectConfigDomainService _projectConfigDomainService;
        private readonly IGoogleSheetExternal _googleSheetExternal;

        public ProjectService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectRepository projectRepository, IDocumentFieldRepository documentFieldRepository, IProjectQuery projectQuery, ProjectDomainService projectDomainService,
            DocumentTypeDomainService documentTypeDomainService, ProjectConfigDomainService projectConfigDomainService, IGoogleSheetExternal googleSheetExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectRepository = projectRepository;
            _documentFieldRepository = documentFieldRepository;
            _projectQuery = projectQuery;
            _projectDomainService = projectDomainService;
            _documentTypeDomainService = documentTypeDomainService;
            _projectConfigDomainService = projectConfigDomainService;
            _googleSheetExternal = googleSheetExternal;
        }

        public async Task<List<GetListProjectDto>> GetList()
        {
            _logger.LogInformation($"{nameof(GetList)}");
            return await _projectQuery.GetList();
        }

        public async Task<List<GetListProjectDropDownDto>> GetProjectDropDown()
        {
            _logger.LogInformation($"{nameof(GetProjectDropDown)}");
            return await _projectQuery.GetProjectDropDown();
        }

        public async Task<BaseResponsePagingDto<GetListProjectDto>> FindPaging(FindPagingProjectDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _projectQuery.FindPaging(dto);
        }

        public async Task<FindProjectByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            var project = await _projectQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            project.DocumentTypeIds = await _projectRepository.GetDocumentTypeIdsAsync(id);
            return project;
        }

        public async Task<FindProjectByIdDto> Create(CreateProjectDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            await _projectDomainService.EnsureUniqueNameAsync(dto.Name);

            var documentTypeIds = dto.DocumentTypeIds.Distinct().ToList();
            await _documentTypeDomainService.EnsureAllExistAsync(documentTypeIds);

            var project = Project.Create(dto.Name, dto.StartedAt, dto.EndedAt, currentUserId, now);
            var account = await GetSpreadsheetAccountAsync();
            var spreadsheet = await _googleSheetExternal.CreateSpreadsheetAsync(account, project.Name, project.GoogleSheetName);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                project.LinkGoogleSheet(spreadsheet.Url);
                _projectRepository.Add(project);
                await _unitOfWork.SaveChangesAsync();

                await AddDocumentTypesAsync(project, documentTypeIds, currentUserId, now);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo dự án {project.Name}",
                    null, ToAuditData(project, documentTypeIds));
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, spreadsheetId={spreadsheet.SpreadsheetId}");
                await transaction.RollbackAsync();
                await _googleSheetExternal.TryDeleteSpreadsheetAsync(account, spreadsheet.SpreadsheetId);
                throw;
            }

            var data = _mapper.Map<FindProjectByIdDto>(project);
            data.DocumentTypeIds = documentTypeIds;
            return data;
        }

        public async Task Update(int id, UpdateProjectDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var project = await _projectRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            var oldDocumentTypeIds = await _projectRepository.GetDocumentTypeIdsAsync(id);
            var oldData = ToAuditData(project, oldDocumentTypeIds);
            var oldName = project.Name;

            await _projectDomainService.EnsureUniqueNameAsync(dto.Name, id);
            var documentTypeIds = dto.DocumentTypeIds.Distinct().ToList();
            await _documentTypeDomainService.EnsureAllExistAsync(documentTypeIds);
            var removedDocumentTypeIds = oldDocumentTypeIds.Except(documentTypeIds).ToList();
            var addedDocumentTypeIds = documentTypeIds.Except(oldDocumentTypeIds).ToList();
            project.Update(dto.Name, dto.StartedAt, dto.EndedAt, dto.Status, currentUserId, now);

            SpreadsheetAccountDto? renamedAccount = null;
            if (project.Name != oldName)
            {
                renamedAccount = await GetSpreadsheetAccountAsync();
                await _googleSheetExternal.RenameSpreadsheetAsync(renamedAccount, project.UrlGoogleSheetSync, project.Name);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _projectRepository.Update(project);
                await _projectRepository.DeleteDocumentFieldsAsync(id, removedDocumentTypeIds);
                await _projectRepository.DeleteDocumentTypesAsync(id, removedDocumentTypeIds);
                await AddDocumentTypesAsync(project, addedDocumentTypeIds, currentUserId, now);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật dự án {project.Name}",
                    oldData, ToAuditData(project, documentTypeIds));
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Update)}, id={id}, oldName={oldName}");
                await transaction.RollbackAsync();
                if (renamedAccount != null)
                {
                    await _googleSheetExternal.TryRenameSpreadsheetAsync(renamedAccount, project.UrlGoogleSheetSync, oldName);
                }
                throw;
            }
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var project = await _projectRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);

            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            var projectDocumentTypes = await _projectRepository.GetDocumentTypesAsync(id);
            var projectDocumentFields = await _projectRepository.GetDocumentFieldsAsync(id);

            project.Delete(currentUserId, now);
            foreach (var projectDocumentType in projectDocumentTypes)
            {
                projectDocumentType.Delete(currentUserId, now);
            }
            foreach (var projectDocumentField in projectDocumentFields)
            {
                projectDocumentField.Delete(currentUserId, now);
            }

            _projectRepository.Update(project);
            AddAuditLog(AuditLogConstants.ACTION_PROJECT_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa dự án {project.Name}",
                ToAuditData(project, projectDocumentTypes.Select(pd => pd.DocumentTypeId)), null);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task AddDocumentTypesAsync(Project project, List<int> documentTypeIds, string? assignedBy, DateTime assignedDate)
        {
            var projectDocumentTypes = project.AssignDocumentTypes(documentTypeIds, assignedBy, assignedDate);
            var documentFields = await _documentFieldRepository.GetByDocumentTypeIdsAsync(documentTypeIds);

            _projectRepository.AddDocumentTypes(projectDocumentTypes);
            _projectRepository.AddDocumentFields(projectDocumentTypes.SelectMany(pd => pd.AssignDocumentFields(documentFields, assignedBy, assignedDate)));
        }

        private async Task<SpreadsheetAccountDto> GetSpreadsheetAccountAsync()
        {
            var values = await _projectConfigDomainService.GetRequiredValuesAsync(ProjectConfigConst.GOOGLE_CLIENT_ID,
                ProjectConfigConst.GOOGLE_CLIENT_SECRET, ProjectConfigConst.GOOGLE_REFRESH_TOKEN, ProjectConfigConst.GOOGLE_FOLDER_ID);

            return new SpreadsheetAccountDto
            {
                ClientId = values[ProjectConfigConst.GOOGLE_CLIENT_ID],
                ClientSecret = values[ProjectConfigConst.GOOGLE_CLIENT_SECRET],
                RefreshToken = values[ProjectConfigConst.GOOGLE_REFRESH_TOKEN],
                FolderId = values[ProjectConfigConst.GOOGLE_FOLDER_ID],
            };
        }

        private static object ToAuditData(Project project, IEnumerable<int> documentTypeIds)
        {
            return new
            {
                project.Id,
                project.Name,
                project.StartedAt,
                project.EndedAt,
                project.Status,
                project.UrlGoogleSheetSync,
                project.GoogleSheetName,
                DocumentTypeIds = documentTypeIds,
            };
        }
    }
}
