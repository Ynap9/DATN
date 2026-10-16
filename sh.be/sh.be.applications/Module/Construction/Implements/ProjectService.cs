using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Spreadsheet;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.domain.Module.Construction.Services;
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
        private readonly IProjectQuery _projectQuery;
        private readonly ProjectDomainService _projectDomainService;
        private readonly ProjectConfigDomainService _projectConfigDomainService;
        private readonly ISpreadsheetWriter _spreadsheetWriter;

        public ProjectService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectRepository projectRepository, IProjectQuery projectQuery, ProjectDomainService projectDomainService,
            ProjectConfigDomainService projectConfigDomainService, ISpreadsheetWriter spreadsheetWriter
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectRepository = projectRepository;
            _projectQuery = projectQuery;
            _projectDomainService = projectDomainService;
            _projectConfigDomainService = projectConfigDomainService;
            _spreadsheetWriter = spreadsheetWriter;
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
            return await _projectQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
        }

        public async Task<FindProjectByIdDto> Create(CreateProjectDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            await _projectDomainService.EnsureUniqueNameAsync(dto.Name);

            var project = Project.Create(dto.Name, dto.StartedAt, dto.EndedAt, getCurrentUserId(), GetVietnamTime());
            var account = await GetSpreadsheetAccountAsync();
            var spreadsheet = await _spreadsheetWriter.CreateSpreadsheetAsync(account, project.Name, project.GoogleSheetName);

            try
            {
                project.LinkGoogleSheet(spreadsheet.Url);
                _projectRepository.Add(project);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo dự án {project.Name}",
                    null, ToAuditData(project));
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, spreadsheetId={spreadsheet.SpreadsheetId}");
                await _spreadsheetWriter.TryDeleteSpreadsheetAsync(account, spreadsheet.SpreadsheetId);
                throw;
            }

            return _mapper.Map<FindProjectByIdDto>(project);
        }

        public async Task Update(int id, UpdateProjectDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var project = await _projectRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            var oldData = ToAuditData(project);
            var oldName = project.Name;

            await _projectDomainService.EnsureUniqueNameAsync(dto.Name, id);
            project.Update(dto.Name, dto.StartedAt, dto.EndedAt, dto.Status, getCurrentUserId(), GetVietnamTime());

            SpreadsheetAccountDto? renamedAccount = null;
            if (project.Name != oldName)
            {
                renamedAccount = await GetSpreadsheetAccountAsync();
                await _spreadsheetWriter.RenameSpreadsheetAsync(renamedAccount, project.UrlGoogleSheetSync, project.Name);
            }

            try
            {
                _projectRepository.Update(project);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật dự án {project.Name}",
                    oldData, ToAuditData(project));
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex) when (renamedAccount != null)
            {
                _logger.LogError(ex, $"{nameof(Update)}, id={id}, oldName={oldName}");
                await _spreadsheetWriter.TryRenameSpreadsheetAsync(renamedAccount, project.UrlGoogleSheetSync, oldName);
                throw;
            }
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var project = await _projectRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);

            project.Delete(getCurrentUserId(), GetVietnamTime());

            _projectRepository.Update(project);
            AddAuditLog(AuditLogConstants.ACTION_PROJECT_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa dự án {project.Name}",
                ToAuditData(project), null);
            await _unitOfWork.SaveChangesAsync();
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

        private static object ToAuditData(Project project)
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
            };
        }
    }
}
