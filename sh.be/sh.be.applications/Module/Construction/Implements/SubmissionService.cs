using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.applications.Module.Construction.Jobs;
using sh.be.domain.Base;
using sh.be.domain.Base.Models;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Construction.Submissions.Repositories;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Linq.Expressions;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionService : BaseService, ISubmissionService
    {
        private readonly ISubmissionQuery _submissionQuery;
        private readonly IProjectQuery _projectQuery;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly QaAssignmentDomainService _qaAssignmentDomainService;
        private readonly ProjectDomainService _projectDomainService;
        private readonly IDataReaderExternal _dataReaderExternal;
        private readonly IExcelReaderExternal _excelReaderExternal;
        private readonly IExcelExternal _excelExternal;
        private readonly IBackgroundJobExternal _backgroundJobExternal;
        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly ISubmissionAssignmentService _submissionAssignmentService;

        public SubmissionService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISubmissionQuery submissionQuery, IProjectQuery projectQuery, ISubmissionRepository submissionRepository, IRoleRepository roleRepository, IUserRepository userRepository,
            SubmissionDomainService submissionDomainService, QaAssignmentDomainService qaAssignmentDomainService, ProjectDomainService projectDomainService,
            IDataReaderExternal dataReaderExternal, IExcelReaderExternal excelReaderExternal, IExcelExternal excelExternal, IBackgroundJobExternal backgroundJobExternal,
            IFileStorageExternal fileStorageExternal, ISubmissionAssignmentService submissionAssignmentService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider, roleRepository, projectDomainService)
        {
            _submissionQuery = submissionQuery;
            _projectQuery = projectQuery;
            _submissionRepository = submissionRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _submissionDomainService = submissionDomainService;
            _qaAssignmentDomainService = qaAssignmentDomainService;
            _projectDomainService = projectDomainService;
            _dataReaderExternal = dataReaderExternal;
            _excelReaderExternal = excelReaderExternal;
            _excelExternal = excelExternal;
            _backgroundJobExternal = backgroundJobExternal;
            _fileStorageExternal = fileStorageExternal;
            _submissionAssignmentService = submissionAssignmentService;
        }

        // Chạy thử import nối thêm hồ sơ mới từ Google Sheet dự án, không ghi DB
        public async Task<ViewVerifyImportAppendSubmissionDto> VerifyImportAppend(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportAppend)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            return await VerifyAppendAsync(dto, ReadProjectGoogleSheetAsync);
        }

        // Import nối thêm hồ sơ mới từ Google Sheet dự án
        public async Task<ViewImportSubmissionResultDto> ImportAppend(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportAppend)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var source = await LoadAppendSheetAsync(dto, ReadProjectGoogleSheetAsync);
            return await ImportAppendAsync(dto, source, getCurrentUserId(),
                new ImportAuditSource(String.Empty, source.Project.UrlGoogleSheetSync, source.Project.GoogleSheetName));
        }

        // Chạy thử import điền ô còn trống từ Google Sheet dự án, không ghi DB
        public async Task<ViewVerifyImportBlankSubmissionDto> VerifyImportBlank(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportBlank)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            return await VerifyBlankAsync(dto, ReadProjectGoogleSheetAsync);
        }

        // Import điền ô còn trống của hồ sơ đã có từ Google Sheet dự án
        public async Task<ViewImportSubmissionResultDto> ImportBlank(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportBlank)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var source = await LoadProjectSheetAsync(dto, null, ReadProjectGoogleSheetAsync);
            return await ImportBlankAsync(dto, source, getCurrentUserId(),
                new ImportAuditSource(String.Empty, source.Project.UrlGoogleSheetSync, source.Project.GoogleSheetName));
        }

        // Chạy thử import ghi đè một trường từ sheet chỉ định, không ghi DB
        public async Task<ViewVerifyImportOverwriteSubmissionDto> VerifyImportOverwrite(ImportOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportOverwrite)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            return await VerifyOverwriteAsync(dto, _dataReaderExternal.ReadTableAsync(dto.Url, dto.SheetName));
        }

        // Import ghi đè một trường của hồ sơ đã có từ sheet chỉ định
        public async Task<ViewImportSubmissionResultDto> ImportOverwrite(ImportOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportOverwrite)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var source = await LoadOverwriteSheetAsync(dto, _dataReaderExternal.ReadTableAsync(dto.Url, dto.SheetName));
            return await ImportOverwriteAsync(dto, source, getCurrentUserId(), new ImportAuditSource($" từ sheet {dto.SheetName}", null, null));
        }

        public async Task<UploadImportExcelResultDto> UploadImportExcel(UploadImportExcelDto dto)
        {
            _logger.LogInformation($"{nameof(UploadImportExcel)}, fileName={dto.File.FileName}, length={dto.File.Length}");
            SubmissionImportDomainService.EnsureValidExcelFile(dto.File.FileName, dto.File.Length);
            var fileKey = SubmissionImportDomainService.BuildExcelFileKey(getCurrentUserId(), GetVietnamTime(), dto.File.FileName);
            await using (var content = dto.File.OpenReadStream())
            {
                await _fileStorageExternal.UploadPrivateAsync(content, fileKey, SubmissionConst.IMPORT_EXCEL_CONTENT_TYPE);
            }

            var sheets = await _excelReaderExternal.GetSheetHeadersAsync(fileKey);
            SubmissionImportDomainService.EnsureAnySheetHasHeader(sheets.Select(sheet => sheet.Headers).ToList());
            return new UploadImportExcelResultDto
            {
                FileKey = fileKey,
                FileName = SubmissionImportDomainService.GetExcelFileName(fileKey),
                Sheets = sheets,
            };
        }

        // Chạy thử import nối thêm từ file Excel đã tải lên, không ghi DB
        public async Task<ViewVerifyImportAppendSubmissionDto> VerifyImportExcelAppend(ImportExcelSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportExcelAppend)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var currentUserId = getCurrentUserId();
            return await VerifyAppendAsync(dto, _ => ReadImportExcelTableAsync(dto, currentUserId));
        }

        // Chạy thử import điền ô trống từ file Excel đã tải lên, không ghi DB
        public async Task<ViewVerifyImportBlankSubmissionDto> VerifyImportExcelBlank(ImportExcelSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportExcelBlank)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var currentUserId = getCurrentUserId();
            return await VerifyBlankAsync(dto, _ => ReadImportExcelTableAsync(dto, currentUserId));
        }

        // Chạy thử import ghi đè một trường từ file Excel đã tải lên, không ghi DB
        public async Task<ViewVerifyImportOverwriteSubmissionDto> VerifyImportExcelOverwrite(ImportExcelOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportExcelOverwrite)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            return await VerifyOverwriteAsync(dto, ReadImportExcelTableAsync(dto, getCurrentUserId()));
        }

        // Xếp job nền import nối thêm từ file Excel, trả jobId
        public async Task<ViewSubmissionImportJobDto> CreateImportExcelAppendJob(ImportExcelSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(CreateImportExcelAppendJob)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var currentUserId = getCurrentUserId();
            return await CreateImportExcelJobAsync(dto, currentUserId, job => job.ExecuteAppendAsync(dto, currentUserId));
        }

        // Xếp job nền import điền ô trống từ file Excel, trả jobId
        public async Task<ViewSubmissionImportJobDto> CreateImportExcelBlankJob(ImportExcelSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(CreateImportExcelBlankJob)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var currentUserId = getCurrentUserId();
            return await CreateImportExcelJobAsync(dto, currentUserId, job => job.ExecuteBlankAsync(dto, currentUserId));
        }

        // Xếp job nền import ghi đè một trường từ file Excel, trả jobId
        public async Task<ViewSubmissionImportJobDto> CreateImportExcelOverwriteJob(ImportExcelOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(CreateImportExcelOverwriteJob)}, dto={JsonSerializer.Serialize(dto)}");
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            var currentUserId = getCurrentUserId();
            return await CreateImportExcelJobAsync(dto, currentUserId, job => job.ExecuteOverwriteAsync(dto, currentUserId));
        }

        public Task<ViewSubmissionImportJobDto> FindImportJob(string jobId)
        {
            _logger.LogInformation($"{nameof(FindImportJob)}, jobId={jobId}");
            var job = _backgroundJobExternal.FindJob(jobId, [BackgroundJobConst.PARAMETER_JOB_TYPE, BackgroundJobConst.PARAMETER_USER_ID,
                BackgroundJobConst.PARAMETER_FILE_NAME]);
            if (job == null
                || job.Parameters[BackgroundJobConst.PARAMETER_JOB_TYPE] != SubmissionConst.IMPORT_JOB_TYPE
                || job.Parameters[BackgroundJobConst.PARAMETER_USER_ID] != getCurrentUserId())
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorImportJobNotFound);
            }

            return Task.FromResult(new ViewSubmissionImportJobDto
            {
                JobId = job.JobId,
                Status = job.Status,
                FileName = job.Parameters[BackgroundJobConst.PARAMETER_FILE_NAME]!,
                ResultMessage = job.Status == BackgroundJobConst.STATUS_SUCCEEDED ? job.Result : null,
                ErrorMessage = job.Status == BackgroundJobConst.STATUS_FAILED ? job.ErrorMessage ?? SubmissionConst.IMPORT_FAILED_MESSAGE : null,
            });
        }

        public async Task<string> ImportExcelAppendFromJob(ImportExcelSubmissionDto dto, string userId)
        {
            _logger.LogInformation($"{nameof(ImportExcelAppendFromJob)}, userId={userId}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadAppendSheetAsync(dto, _ => ReadImportExcelTableAsync(dto, userId));
            var result = await ImportAppendAsync(dto, source, userId, ToExcelAuditSource(dto));
            return result.Message;
        }

        public async Task<string> ImportExcelBlankFromJob(ImportExcelSubmissionDto dto, string userId)
        {
            _logger.LogInformation($"{nameof(ImportExcelBlankFromJob)}, userId={userId}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadProjectSheetAsync(dto, null, _ => ReadImportExcelTableAsync(dto, userId));
            var result = await ImportBlankAsync(dto, source, userId, ToExcelAuditSource(dto));
            return result.Message;
        }

        public async Task<string> ImportExcelOverwriteFromJob(ImportExcelOverwriteSubmissionDto dto, string userId)
        {
            _logger.LogInformation($"{nameof(ImportExcelOverwriteFromJob)}, userId={userId}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadOverwriteSheetAsync(dto, ReadImportExcelTableAsync(dto, userId));
            var result = await ImportOverwriteAsync(dto, source, userId, ToExcelAuditSource(dto));
            return result.Message;
        }

        // Phân trang hồ sơ theo bộ lọc, chỉ văn bản mình giữ nếu bị giới hạn phân việc
        public async Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            return await _submissionQuery.FindPaging(dto, restrictedUserId);
        }

        // Chi tiết hồ sơ kèm vị trí trong danh sách đã lọc
        public async Task<ViewSubmissionDetailDto> FindById(int id, FindSubmissionDetailDto dto)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var currentUserId = getCurrentUserId();
            await _submissionAssignmentService.EnsureCanViewAsync(await _submissionDomainService.EnsureSubmissionExistsAsync(id), currentUserId);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, currentUserId);
            var detail = await _submissionQuery.FindById(id, dto, dto.IncludePosition, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            if (detail.ProjectId != dto.ProjectId || detail.DocumentTypeId != dto.DocumentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
            return detail;
        }

        // Hồ sơ kế sau trong danh sách đã lọc
        public async Task<ViewSubmissionDetailDto> FindNext(int id, SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(FindNext)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            var nextId = await _submissionQuery.FindNextId(id, dto, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange);
            return await _submissionQuery.FindById(nextId, dto, false, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        // Hồ sơ liền trước trong danh sách đã lọc
        public async Task<ViewSubmissionDetailDto> FindPrevious(int id, SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(FindPrevious)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            var previousId = await _submissionQuery.FindPreviousId(id, dto, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange);
            return await _submissionQuery.FindById(previousId, dto, false, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        // Đếm hồ sơ theo trạng thái kiểm tra trong danh sách đã lọc
        public async Task<ViewCountSubmissionDto> Count(SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(Count)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            return await _submissionQuery.Count(dto, restrictedUserId);
        }

        // Kiểm tra hồ sơ: lưu dữ liệu sửa + log + audit, chuyển văn bản vừa kiểm cho QA phụ trách; dự án không QA thì tự vào nghiệm thu
        public async Task<ViewCheckSubmissionResultDto> Check(CheckSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(Check)}, dto={JsonSerializer.Serialize(dto)}");
            var submission = await _submissionDomainService.EnsureSubmissionExistsAsync(dto.Id);
            submission.EnsureBelongsTo(dto.ProjectId, dto.DocumentTypeId);

            var currentUserId = getCurrentUserId();
            await _submissionAssignmentService.EnsureCanCheckAsync(submission, currentUserId);
            var isSuperAdmin = PermissionDomainService.IsSuperAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, isSuperAdmin);
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var existingDatas = await _submissionRepository.GetDatasBySubmissionIdAsync(dto.Id);
            var userName = (await _userRepository.FindByIdAsync(currentUserId))?.UserName ?? currentUserId;

            var now = GetVietnamTime();
            var inputs = dto.Datas.Select(data => new SubmissionDataInput(data.DocumentFieldId, data.Data)).ToList();
            var plan = SubmissionDomainService.BuildCheckPlan(submission, inputs, mappings, existingDatas, currentUserId, now);
            var log = submission.Check(currentUserId, userName, dto.Note, plan.ChangedDataCount, now);

            _submissionRepository.AddDatas(plan.DatasToInsert);
            _submissionRepository.AddLog(log);
            AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_CHECK, AuditLogConstants.ACTION_TYPE_UPDATE,
                $"Kiểm tra hồ sơ {submission.FileName} (Id = {dto.Id}) của dự án {project.Name}, sửa {plan.ChangedDataCount} trường",
                new { StatusCheck = log.OldStatus, Datas = plan.Changes.Select(change => new { change.DocumentFieldId, Data = change.OldData }) },
                new { StatusCheck = log.NewStatus, Datas = plan.Changes.Select(change => new { change.DocumentFieldId, Data = change.NewData }), log.Note });

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.SaveChangesAsync();
                await _submissionAssignmentService.AssignCheckedToQaAsync(submission, currentUserId, now);
                await _unitOfWork.SaveChangesAsync();

                // Dự án không QA: văn bản cuối cùng được kiểm thì dự án tự vào nghiệm thu; dự án có QA chỉ tự chuyển ở ReviewQa
                if (!project.IsQaEnabled)
                {
                    await TryStartAcceptanceAutomaticallyAsync(project, currentUserId, now);
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Check)}, id={dto.Id}");
                await transaction.RollbackAsync();
                throw;
            }

            return new ViewCheckSubmissionResultDto
            {
                SubmissionId = dto.Id,
                StatusCheck = submission.StatusCheck,
                StatusQa = submission.StatusQa,
                ChangedDataCount = plan.ChangedDataCount,
                LogId = log.Id,
            };
        }

        // Phân trang lịch sử kiểm tra của một hồ sơ
        public async Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int id, FindPagingSubmissionLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindLogPaging)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionAssignmentService.EnsureCanViewAsync(await _submissionDomainService.EnsureSubmissionExistsAsync(id), getCurrentUserId());
            return await _submissionQuery.FindLogPaging(id, dto);
        }

        // Phân trang hồ sơ màn QA kèm số đếm theo trạng thái QA
        public async Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            return await _submissionQuery.FindQaPaging(dto, restrictedUserId);
        }

        // Chi tiết hồ sơ màn QA kèm vị trí trong danh sách đã lọc
        public async Task<ViewQaSubmissionDetailDto> FindQaById(int id, FindQaSubmissionDetailDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaById)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var currentUserId = getCurrentUserId();
            await _submissionAssignmentService.EnsureCanViewAsync(await _submissionDomainService.EnsureSubmissionExistsAsync(id), currentUserId);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, currentUserId);
            var detail = await _submissionQuery.FindQaById(id, dto, dto.IncludePosition, restrictedUserId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            if (detail.ProjectId != dto.ProjectId || detail.DocumentTypeId != dto.DocumentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
            return detail;
        }

        // Chấm QA Đạt / Không đạt (dự án không QA → 345): lưu log QA, cập nhật người giữ / phiếu, chốt phiếu FULL, tự vào nghiệm thu khi đủ
        public async Task<ViewQaResultDto> ReviewQa(ReviewQaSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ReviewQa)}, dto={JsonSerializer.Serialize(dto)}");
            var submission = await _submissionDomainService.EnsureSubmissionExistsAsync(dto.Id);
            submission.EnsureBelongsTo(dto.ProjectId, dto.DocumentTypeId);

            var currentUserId = getCurrentUserId();
            var roleNames = await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId);
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, PermissionDomainService.IsSuperAdmin(roleNames));
            project.EnsureQaEnabled();
            var qaTicket = await _qaAssignmentDomainService.EnsureReviewableAsync(submission, currentUserId, PermissionDomainService.IsAdmin(roleNames));
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var userName = (await _userRepository.FindByIdAsync(currentUserId))?.UserName ?? currentUserId;

            var now = GetVietnamTime();
            var qaLog = submission.ReviewQa(dto.Result, dto.Reason, dto.WrongDocumentFieldIds,
                mappings.Select(mapping => mapping.DocumentFieldId).ToList(), currentUserId, userName, now);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _submissionRepository.AddQaLog(qaLog);
                await _unitOfWork.SaveChangesAsync();

                var wrongFields = qaLog.CreateWrongFields(dto.WrongDocumentFieldIds, currentUserId, now);
                _submissionRepository.AddQaLogFields(wrongFields);
                AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_QA, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"QA hồ sơ {submission.FileName} (Id = {dto.Id}) của dự án {project.Name}: {(qaLog.NewStatusQa == SubmissionConst.QA_PASSED ? "Đạt" : "Không đạt")}",
                    new { qaLog.OldStatusCheck, qaLog.OldStatusQa },
                    new { qaLog.NewStatusCheck, qaLog.NewStatusQa, qaLog.Reason, WrongDocumentFieldIds = wrongFields.Select(field => field.DocumentFieldId) });
                await _unitOfWork.SaveChangesAsync();

                await _submissionAssignmentService.ApplyQaResultToAssignmentAsync(submission, qaLog.NewStatusQa, currentUserId, now);
                await _unitOfWork.SaveChangesAsync();

                if (qaTicket != null && await _qaAssignmentDomainService.TryPassFullTicketAsync(qaTicket, currentUserId, now))
                {
                    AddAuditLog(AuditLogConstants.ACTION_QA_TICKET_CONCLUDE, AuditLogConstants.ACTION_TYPE_UPDATE,
                        $"Phiếu #{qaTicket.Id} FULL ĐẠT ({qaTicket.DocumentCount} văn bản) của dự án {project.Name}",
                        new { qaTicket.Id, Status = QaAssignmentConst.TICKET_STATUS_ACTIVE }, new { qaTicket.Id, qaTicket.Status, qaTicket.ConcludedDate });
                    await _unitOfWork.SaveChangesAsync();
                }

                // Văn bản cuối cùng QA đạt thì dự án tự vào nghiệm thu; dự án có QA chỉ tự chuyển ở đây (không QA thì ở Check), không tự sang Xong
                if (qaLog.NewStatusQa == SubmissionConst.QA_PASSED)
                {
                    await TryStartAcceptanceAutomaticallyAsync(project, currentUserId, now);
                }

                await transaction.CommitAsync();

                return new ViewQaResultDto
                {
                    SubmissionId = dto.Id,
                    StatusCheck = submission.StatusCheck,
                    StatusQa = submission.StatusQa,
                    QaReason = submission.QaReason,
                    WrongDocumentFieldIds = wrongFields.Select(field => field.DocumentFieldId).ToList(),
                    QaLogId = qaLog.Id,
                    QaTicketId = qaTicket?.Id,
                    TicketStatus = qaTicket?.Status,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ReviewQa)}, id={dto.Id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Phân trang lịch sử QA của một hồ sơ
        public async Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int id, FindPagingSubmissionQaLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaLogPaging)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionAssignmentService.EnsureCanViewAsync(await _submissionDomainService.EnsureSubmissionExistsAsync(id), getCurrentUserId());
            return await _submissionQuery.FindQaLogPaging(id, dto);
        }

        // Liệt kê giá trị của một trường trong danh sách đã lọc, dùng cho ô lọc
        public async Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto)
        {
            _logger.LogInformation($"{nameof(FindFieldValues)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            await _submissionDomainService.EnsureDocumentFieldInProjectAsync(dto.ProjectId, dto.DocumentTypeId, dto.DocumentFieldId);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            return await _submissionQuery.FindFieldValues(dto, restrictedUserId);
        }

        // Cây hồ sơ / hộp kèm số văn bản theo bộ lọc
        public async Task<ViewSubmissionTreeDto> FindTree(FindSubmissionTreeDto dto)
        {
            _logger.LogInformation($"{nameof(FindTree)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var restrictedUserId = await _submissionAssignmentService.ResolveRestrictedUserIdAsync(dto.ProjectId, dto.DocumentTypeId, getCurrentUserId());
            return await _submissionQuery.FindTree(dto, restrictedUserId);
        }

        public async Task<BaseResponsePagingDto<GetListProjectSubmissionLogDto>> FindProjectLogPaging(FindPagingProjectSubmissionLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindProjectLogPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.FromDate.HasValue && dto.ToDate.HasValue && dto.FromDate.Value.Date > dto.ToDate.Value.Date)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Từ ngày không được sau đến ngày");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.FindProjectLogPaging(dto);
        }

        public async Task<ViewSubmissionSummaryDto> FindSummary(FindSubmissionSummaryDto dto)
        {
            _logger.LogInformation($"{nameof(FindSummary)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.FindSummary(dto.ProjectId, dto.DocumentTypeId);
        }

        public async Task<ViewQaStatsSummaryDto> FindQaStatsSummary(FindQaStatsDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaStatsSummary)}, dto={JsonSerializer.Serialize(dto)}");
            var (fromDate, toDate) = NormalizeQaStatsDateRange(dto.FromDate, dto.ToDate);
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);

            var summary = await _submissionQuery.FindQaStatsSummary(dto, fromDate, toDate.AddDays(1));
            summary.FromDate = fromDate;
            summary.ToDate = toDate;
            return summary;
        }

        public async Task<List<GetListQaFieldStatsDto>> FindQaFieldStats(FindQaStatsDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaFieldStats)}, dto={JsonSerializer.Serialize(dto)}");
            var (fromDate, toDate) = NormalizeQaStatsDateRange(dto.FromDate, dto.ToDate);
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.FindQaFieldStats(dto, fromDate, toDate.AddDays(1));
        }

        public async Task<BaseResponsePagingDto<GetListQaEntryUserStatsDto>> FindQaEntryUserStatsPaging(FindPagingQaEntryUserStatsDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaEntryUserStatsPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            if (dto.OrderBy is not (null or SubmissionConst.QA_STATS_ORDER_REVIEW or SubmissionConst.QA_STATS_ORDER_ACCURACY))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest,
                    $"orderBy chỉ nhận {SubmissionConst.QA_STATS_ORDER_REVIEW} hoặc {SubmissionConst.QA_STATS_ORDER_ACCURACY}");
            }
            var (fromDate, toDate) = NormalizeQaStatsDateRange(dto.FromDate, dto.ToDate);
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            return await _submissionQuery.FindQaEntryUserStatsPaging(dto, fromDate, toDate.AddDays(1));
        }

        // Trục thời gian gộp các lần kiểm tra và QA của một hồ sơ
        public async Task<List<GetListSubmissionTimelineDto>> FindTimeline(int id)
        {
            _logger.LogInformation($"{nameof(FindTimeline)}, id={id}");
            await _submissionAssignmentService.EnsureCanViewAsync(await _submissionDomainService.EnsureSubmissionExistsAsync(id), getCurrentUserId());
            return await _submissionQuery.FindTimeline(id);
        }

        // Admin xoá mềm nhiều hồ sơ kèm dữ liệu (đã kiểm tra chỉ SuperAdmin), khoá cặp như import, gỡ người giữ, dự án đủ điều kiện thì tự vào nghiệm thu
        public async Task<ViewDeleteSubmissionResultDto> Delete(DeleteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(Delete)}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var roleNames = await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId);
            SubmissionDomainService.EnsureCanDelete(PermissionDomainService.IsAdmin(roleNames));
            var isSuperAdmin = PermissionDomainService.IsSuperAdmin(roleNames);
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, isSuperAdmin);
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _submissionDomainService.LockImportAsync(dto.ProjectId, dto.DocumentTypeId);
                var submissions = await _submissionDomainService.GetSubmissionsToDeleteAsync(dto.ProjectId, dto.DocumentTypeId, dto.Ids);
                await _qaAssignmentDomainService.EnsureDeletableAsync(submissions);
                _submissionDomainService.EnsureCheckedDeletable(submissions, isSuperAdmin);
                foreach (var submission in submissions)
                {
                    submission.Delete(currentUserId, now);
                }

                var deletedDataCount = await _submissionRepository.SoftDeleteDatasBySubmissionIdsAsync(
                    submissions.Select(submission => submission.Id).ToList(), currentUserId, now);
                AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_DELETE, AuditLogConstants.ACTION_TYPE_DELETE,
                    $"Xóa {submissions.Count} hồ sơ của dự án {project.Name}, loại văn bản Id = {dto.DocumentTypeId}",
                    submissions.Select(submission => new { submission.Id, submission.FileName, submission.ProfileRange, submission.Box, submission.StatusCheck, submission.StatusQa }));
                await _unitOfWork.SaveChangesAsync();

                // Gỡ người giữ, trừ phiếu, xét DONE trên văn bản vừa xoá; rồi mới đếm nghiệm thu vì đếm trên DB đã lưu xoá
                await _submissionAssignmentService.ReleaseDeletedSubmissionsAsync(submissions, currentUserId, now);
                await _unitOfWork.SaveChangesAsync();
                await TryStartAcceptanceAutomaticallyAsync(project, currentUserId, now);

                await transaction.CommitAsync();
                return new ViewDeleteSubmissionResultDto
                {
                    DeletedSubmissionCount = submissions.Count,
                    DeletedDataCount = deletedDataCount,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Delete)}, projectId={dto.ProjectId}, documentTypeId={dto.DocumentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Xuất Excel hồ sơ theo bộ lọc, trả file ngay
        public async Task<ExportFileDto> ExportExcel(ExportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ExportExcel)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            dto.Columns = SubmissionDomainService.NormalizeExportColumns(dto.Columns);
            var table = await _submissionQuery.FindExportTable(dto);
            return _excelExternal.CreateTable($"{table.ProjectName}_{table.DocumentTypeName}_{GetVietnamTime():yyyyMMddHHmm}", table.DocumentTypeName,
                table.Headers, table.Rows);
        }

        // Job nền dựng file Excel hồ sơ; quyền và phiếu QA đã kiểm lúc CreateExportJob vì job không có user từ token
        public async Task<ExportFileDto> ExportExcelFromJob(ExportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ExportExcelFromJob)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            var table = await _submissionQuery.FindExportTable(dto);
            return _excelExternal.CreateTable($"{table.ProjectName}_{table.DocumentTypeName}_{GetVietnamTime():yyyyMMddHHmm}", table.DocumentTypeName,
                table.Headers, table.Rows);
        }

        // Xếp job nền xuất Excel hồ sơ theo bộ lọc, trả jobId
        public async Task<ViewSubmissionExportJobDto> CreateExportJob(ExportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(CreateExportJob)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureAdminOrProjectLeaderAsync(dto.ProjectId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            dto.Columns = SubmissionDomainService.NormalizeExportColumns(dto.Columns);
            var names = await _submissionQuery.FindExportName(dto.ProjectId, dto.DocumentTypeId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            if (!_backgroundJobExternal.HasServerForQueue(BackgroundJobConst.QUEUE_EXPORT))
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorBackgroundJobUnavailable);
            }

            var now = GetVietnamTime();
            var fileName = $"{names.ProjectName}_{names.DocumentTypeName}_{now:yyyyMMddHHmm}.xlsx";
            var objectKey = $"{SubmissionConst.EXPORT_OBJECT_KEY_PREFIX}/{now:yyyyMMdd}/{Guid.NewGuid():N}.xlsx";
            var jobId = _backgroundJobExternal.Enqueue<SubmissionExportJob>(job => job.ExecuteAsync(dto, objectKey), BackgroundJobConst.QUEUE_EXPORT,
                new Dictionary<string, string>
                {
                    [BackgroundJobConst.PARAMETER_JOB_TYPE] = SubmissionConst.EXPORT_JOB_TYPE,
                    [BackgroundJobConst.PARAMETER_USER_ID] = getCurrentUserId(),
                    [BackgroundJobConst.PARAMETER_FILE_NAME] = fileName,
                    [BackgroundJobConst.PARAMETER_OBJECT_KEY] = objectKey,
                });

            return new ViewSubmissionExportJobDto
            {
                JobId = jobId,
                Status = BackgroundJobConst.STATUS_WAITING,
                FileName = fileName,
            };
        }

        public Task<ViewSubmissionExportJobDto> FindExportJob(string jobId)
        {
            _logger.LogInformation($"{nameof(FindExportJob)}, jobId={jobId}");
            var job = _backgroundJobExternal.FindJob(jobId, [BackgroundJobConst.PARAMETER_JOB_TYPE, BackgroundJobConst.PARAMETER_USER_ID,
                BackgroundJobConst.PARAMETER_FILE_NAME, BackgroundJobConst.PARAMETER_OBJECT_KEY]);
            if (job == null
                || job.Parameters[BackgroundJobConst.PARAMETER_JOB_TYPE] != SubmissionConst.EXPORT_JOB_TYPE
                || job.Parameters[BackgroundJobConst.PARAMETER_USER_ID] != getCurrentUserId())
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorExportJobNotFound);
            }

            var fileName = job.Parameters[BackgroundJobConst.PARAMETER_FILE_NAME]!;
            return Task.FromResult(new ViewSubmissionExportJobDto
            {
                JobId = job.JobId,
                Status = job.Status,
                FileName = fileName,
                DownloadUrl = job.Status == BackgroundJobConst.STATUS_SUCCEEDED
                    ? _fileStorageExternal.GetTemporaryUrl(job.Parameters[BackgroundJobConst.PARAMETER_OBJECT_KEY]!, fileName,
                        TimeSpan.FromMinutes(SubmissionConst.EXPORT_DOWNLOAD_URL_MINUTES))
                    : null,
                ErrorMessage = job.Status == BackgroundJobConst.STATUS_FAILED ? job.ErrorMessage ?? SubmissionConst.EXPORT_FAILED_MESSAGE : null,
            });
        }

        private async Task EnsureQaTicketFilterVisibleAsync(ISubmissionFilter filter)
        {
            if (filter.QaTicketId is int qaTicketId)
            {
                await _qaAssignmentDomainService.EnsureVisibleTicketAsync(qaTicketId, getCurrentUserId());
            }
        }

        private async Task<ViewVerifyImportAppendSubmissionDto> VerifyAppendAsync(ImportSubmissionDto dto, Func<Project, Task<SheetTable>> readTableAsync)
        {
            var source = await LoadAppendSheetAsync(dto, readTableAsync);
            var plan = await BuildAppendPlanAsync(dto, source, getCurrentUserId(), GetVietnamTime());

            return new ViewVerifyImportAppendSubmissionDto
            {
                TotalDataFields = plan.NonEmptyCellCount,
                TotalAppendRowCount = plan.NewSubmissions.Count,
                TotalFieldCount = plan.MappedFieldCount,
                TotalSubmission = plan.ExistingSubmissionCount,
                TotalSubmissionGoogleSheet = plan.DistinctSheetKeyCount,
            };
        }

        private async Task<ViewVerifyImportBlankSubmissionDto> VerifyBlankAsync(ImportSubmissionDto dto, Func<Project, Task<SheetTable>> readTableAsync)
        {
            var source = await LoadProjectSheetAsync(dto, null, readTableAsync);
            var plan = await BuildBlankPlanAsync(dto, source, getCurrentUserId(), GetVietnamTime());

            return new ViewVerifyImportBlankSubmissionDto
            {
                TotalBlankCount = plan.ChangedDataCount,
                TotalFieldCount = plan.MappedFieldCount,
            };
        }

        private async Task<ViewVerifyImportOverwriteSubmissionDto> VerifyOverwriteAsync<TDto>(TDto dto, Task<SheetTable> tableTask)
            where TDto : ImportSubmissionDto, IImportOverwriteOptions
        {
            var source = await LoadOverwriteSheetAsync(dto, tableTask);
            var plan = await BuildOverwritePlanAsync(dto, source, getCurrentUserId(), GetVietnamTime());

            return new ViewVerifyImportOverwriteSubmissionDto
            {
                TotalOverWriteCount = plan.ChangedDataCount,
                TotalSkippedChecked = plan.SkippedCheckedCount,
            };
        }

        private async Task<ViewImportSubmissionResultDto> ImportAppendAsync(ImportSubmissionDto dto, ProjectSheetSource source, string userId, ImportAuditSource auditSource)
        {
            var now = GetVietnamTime();
            var plan = await ImportInTransactionAsync(dto, nameof(ImportAppendAsync),
                () => BuildAppendPlanAsync(dto, source, userId, now),
                async appendPlan =>
                {
                    if (!appendPlan.HasChanges)
                    {
                        return;
                    }

                    await _submissionRepository.BulkInsertSubmissionsAsync(appendPlan.NewSubmissions.Select(draft => draft.Submission).ToList());
                    await _submissionRepository.BulkInsertDatasAsync(appendPlan.NewSubmissions.SelectMany(draft => draft.AssignDatasToSubmission()).ToList());
                    AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_APPEND, AuditLogConstants.ACTION_TYPE_CREATE,
                        $"Import thêm mới {appendPlan.NewSubmissions.Count} hồ sơ, {appendPlan.NewDataCount} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}"
                            + auditSource.Description,
                        null, new
                        {
                            dto.ProjectId,
                            dto.DocumentTypeId,
                            auditSource.SheetUrl,
                            auditSource.SheetName,
                            NewSubmissionCount = appendPlan.NewSubmissions.Count,
                            appendPlan.NewDataCount,
                            appendPlan.MappedFieldCount,
                        }, userId);
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Import hoàn tất. Hồ sơ thêm mới: {plan.NewSubmissions.Count}, "
                    + $"Dữ liệu thêm mới: {plan.NewDataCount}, Trường đã map: {plan.MappedFieldCount}, Ô dữ liệu có giá trị: {plan.NonEmptyCellCount}",
                TotalDataFields = plan.NonEmptyCellCount,
            };
        }

        private async Task<ViewImportSubmissionResultDto> ImportBlankAsync(ImportSubmissionDto dto, ProjectSheetSource source, string userId, ImportAuditSource auditSource)
        {
            var now = GetVietnamTime();
            var plan = await ImportInTransactionAsync(dto, nameof(ImportBlankAsync),
                () => BuildBlankPlanAsync(dto, source, userId, now),
                async blankPlan =>
                {
                    if (!blankPlan.HasChanges)
                    {
                        return;
                    }

                    await _submissionRepository.BulkUpdateDatasAsync(blankPlan.DatasToUpdate.ToList());
                    await _submissionRepository.BulkInsertDatasAsync(blankPlan.DatasToInsert.ToList());
                    AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_BLANK, AuditLogConstants.ACTION_TYPE_UPDATE,
                        $"Bổ sung {blankPlan.DatasToUpdate.Count} ô trống, thêm {blankPlan.DatasToInsert.Count} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}"
                            + auditSource.Description,
                        null, new
                        {
                            dto.ProjectId,
                            dto.DocumentTypeId,
                            auditSource.SheetUrl,
                            auditSource.SheetName,
                            UpdatedDataCount = blankPlan.DatasToUpdate.Count,
                            NewDataCount = blankPlan.DatasToInsert.Count,
                            blankPlan.MappedFieldCount,
                        }, userId);
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Import bổ sung hoàn tất. Ô trống đã điền: {plan.DatasToUpdate.Count}, "
                    + $"Dữ liệu thêm mới: {plan.DatasToInsert.Count}",
                TotalDataFields = plan.ChangedDataCount,
            };
        }

        private async Task<ViewImportSubmissionResultDto> ImportOverwriteAsync<TDto>(TDto dto, OverwriteSheetSource source, string userId, ImportAuditSource auditSource)
            where TDto : ImportSubmissionDto, IImportOverwriteOptions
        {
            var now = GetVietnamTime();
            var plan = await ImportInTransactionAsync(dto, nameof(ImportOverwriteAsync),
                () => BuildOverwritePlanAsync(dto, source, userId, now),
                async overwritePlan =>
                {
                    if (!overwritePlan.HasChanges)
                    {
                        return;
                    }

                    await _submissionRepository.BulkUpdateDatasAsync(overwritePlan.DatasToUpdate.ToList());
                    await _submissionRepository.BulkInsertDatasAsync(overwritePlan.DatasToInsert.ToList());
                    AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_OVERWRITE, AuditLogConstants.ACTION_TYPE_UPDATE,
                        $"Ghi đè {overwritePlan.DatasToUpdate.Count}, thêm {overwritePlan.DatasToInsert.Count} dữ liệu trường Id = {dto.DocumentFieldId} của dự án {source.Project.Name}, "
                            + $"loại văn bản Id = {dto.DocumentTypeId}{auditSource.Description}",
                        overwritePlan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.OldData }),
                        overwritePlan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.NewData }),
                        userId);
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Ghi đè hoàn tất. Đã ghi đè: {plan.DatasToUpdate.Count}, "
                    + $"Thêm mới: {plan.DatasToInsert.Count}, Bỏ qua do đã kiểm tra: {plan.SkippedCheckedCount}",
                TotalDataFields = plan.ChangedDataCount,
            };
        }

        private async Task<ViewSubmissionImportJobDto> CreateImportExcelJobAsync(ImportExcelSubmissionDto dto, string currentUserId,
            Expression<Func<SubmissionImportJob, Task>> job)
        {
            // Kiểm sớm để báo lỗi ngay khi bấm import; job vẫn kiểm lại toàn bộ vì dự án có thể đổi trạng thái trong lúc job chờ.
            await _submissionDomainService.EnsureImportableProjectAsync(dto.ProjectId);
            await _submissionDomainService.EnsureDocumentTypeInProjectAsync(dto.ProjectId, dto.DocumentTypeId);
            SubmissionImportDomainService.EnsureExcelFileOwnedBy(dto.FileKey, currentUserId);
            if (!_backgroundJobExternal.HasServerForQueue(BackgroundJobConst.QUEUE_IMPORT))
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorBackgroundJobUnavailable);
            }

            var fileName = SubmissionImportDomainService.GetExcelFileName(dto.FileKey);
            var jobId = _backgroundJobExternal.Enqueue(job, BackgroundJobConst.QUEUE_IMPORT,
                new Dictionary<string, string>
                {
                    [BackgroundJobConst.PARAMETER_JOB_TYPE] = SubmissionConst.IMPORT_JOB_TYPE,
                    [BackgroundJobConst.PARAMETER_USER_ID] = currentUserId,
                    [BackgroundJobConst.PARAMETER_FILE_NAME] = fileName,
                });

            return new ViewSubmissionImportJobDto
            {
                JobId = jobId,
                Status = BackgroundJobConst.STATUS_WAITING,
                FileName = fileName,
            };
        }

        private async Task<ProjectSheetSource> LoadAppendSheetAsync(ImportSubmissionDto dto, Func<Project, Task<SheetTable>> readTableAsync)
        {
            var source = await LoadProjectSheetAsync(dto, dto.LinkColumnImport, readTableAsync);
            SubmissionImportDomainService.EnsureFileUrlsPresent(source.Table, source.Columns);
            return source;
        }

        private async Task<ProjectSheetSource> LoadProjectSheetAsync(ImportSubmissionDto dto, string? fileUrlColumn, Func<Project, Task<SheetTable>> readTableAsync)
        {
            var project = await _submissionDomainService.EnsureImportableProjectAsync(dto.ProjectId);
            await _submissionDomainService.EnsureDocumentTypeInProjectAsync(dto.ProjectId, dto.DocumentTypeId);
            var tableTask = readTableAsync(project);
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var table = await tableTask;

            var columns = ResolveSheetColumns(table, dto, fileUrlColumn);
            var mappedColumns = SubmissionImportDomainService.ResolveMappedColumns(table.Headers, mappings);
            return new ProjectSheetSource(project, table, columns, mappedColumns);
        }

        private async Task<OverwriteSheetSource> LoadOverwriteSheetAsync<TDto>(TDto dto, Task<SheetTable> tableTask)
            where TDto : ImportSubmissionDto, IImportOverwriteOptions
        {
            var project = await _submissionDomainService.EnsureImportableProjectAsync(dto.ProjectId);
            await _submissionDomainService.EnsureDocumentTypeInProjectAsync(dto.ProjectId, dto.DocumentTypeId);
            var mapping = await _submissionDomainService.EnsureDocumentFieldInProjectAsync(dto.ProjectId, dto.DocumentTypeId, dto.DocumentFieldId);
            var table = await tableTask;

            var columns = ResolveSheetColumns(table, dto, null);
            var columnImportIndex = SubmissionImportDomainService.ResolveSingleColumn(table.Headers, dto.ColumnImport);
            return new OverwriteSheetSource(project, table, columns, mapping, columnImportIndex);
        }

        private Task<SheetTable> ReadProjectGoogleSheetAsync(Project project)
        {
            return _dataReaderExternal.ReadTableAsync(project.UrlGoogleSheetSync, project.GoogleSheetName);
        }

        private static ImportAuditSource ToExcelAuditSource(ImportExcelSubmissionDto dto)
        {
            return new ImportAuditSource($" từ file Excel {SubmissionImportDomainService.GetExcelFileName(dto.FileKey)}, sheet {dto.SheetName}", null, dto.SheetName);
        }

        // Nhận userId thay vì getCurrentUserId() để job import chạy nền (không có HttpContext) dùng chung đường kiểm này.
        private async Task<SheetTable> ReadImportExcelTableAsync(ImportExcelSubmissionDto dto, string userId)
        {
            SubmissionImportDomainService.EnsureExcelFileOwnedBy(dto.FileKey, userId);
            var table = await _excelReaderExternal.ReadTableAsync(dto.FileKey, dto.SheetName);
            SubmissionImportDomainService.EnsureExcelRowLimit(table);
            return table;
        }

        private static SubmissionImportColumns ResolveSheetColumns(SheetTable table, ImportSubmissionDto dto, string? fileUrlColumn)
        {
            SubmissionImportDomainService.EnsureHasData(table);
            var columns = SubmissionImportDomainService.ResolveColumns(table.Headers, dto.FileColumnImport, dto.ProfileRange, dto.Box, fileUrlColumn);
            SubmissionImportDomainService.EnsureNoDuplicateKeys(table, columns);
            return columns;
        }

        private async Task<AppendImportPlan> BuildAppendPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, string? importedBy, DateTime importedDate)
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            return SubmissionImportDomainService.BuildAppendPlan(source.Table, source.Columns, source.MappedColumns, snapshots,
                dto.ProjectId, dto.DocumentTypeId, importedBy, importedDate);
        }

        private async Task<BlankImportPlan> BuildBlankPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, string? importedBy, DateTime importedDate)
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            var documentFieldIds = source.MappedColumns.Select(column => column.DocumentFieldId).ToList();
            var existingDatas = await _submissionRepository.GetDatasAsync(dto.ProjectId, dto.DocumentTypeId, documentFieldIds);
            return SubmissionImportDomainService.BuildBlankPlan(source.Table, source.Columns, source.MappedColumns, snapshots, existingDatas,
                dto.ProjectId, dto.DocumentTypeId, importedBy, importedDate);
        }

        private async Task<OverwriteImportPlan> BuildOverwritePlanAsync<TDto>(TDto dto, OverwriteSheetSource source, string? importedBy, DateTime importedDate)
            where TDto : ImportSubmissionDto, IImportOverwriteOptions
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            var existingDatas = await _submissionRepository.GetDatasAsync(dto.ProjectId, dto.DocumentTypeId, [dto.DocumentFieldId]);
            return SubmissionImportDomainService.BuildOverwritePlan(source.Table, source.Columns, source.ColumnImportIndex, source.Mapping, snapshots,
                existingDatas, dto.OverwriteChecked, importedBy, importedDate);
        }

        private async Task<TPlan> ImportInTransactionAsync<TPlan>(ImportSubmissionDto dto, string methodName, Func<Task<TPlan>> buildPlanAsync,
            Func<TPlan, Task> savePlanAsync)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _submissionDomainService.LockImportAsync(dto.ProjectId, dto.DocumentTypeId);
                var plan = await buildPlanAsync();
                await savePlanAsync(plan);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{methodName}, projectId={dto.ProjectId}, documentTypeId={dto.DocumentTypeId}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private (DateTime FromDate, DateTime ToDate) NormalizeQaStatsDateRange(DateTime? fromDate, DateTime? toDate)
        {
            var normalizedToDate = (toDate ?? GetVietnamTime()).Date;
            var normalizedFromDate = (fromDate ?? normalizedToDate.AddDays(-(SubmissionConst.QA_STATS_DEFAULT_DAYS - 1))).Date;
            if (normalizedFromDate > normalizedToDate)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Từ ngày không được sau đến ngày");
            }
            if ((normalizedToDate - normalizedFromDate).Days + 1 > SubmissionConst.QA_STATS_MAX_DAYS)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Khoảng thời gian tối đa {SubmissionConst.QA_STATS_MAX_DAYS} ngày");
            }
            return (normalizedFromDate, normalizedToDate);
        }

        // Dự án đủ điều kiện thì tự vào nghiệm thu và ghi audit "Bắt đầu nghiệm thu" tự động; gọi trong transaction của Check / ReviewQa / Delete
        private async Task TryStartAcceptanceAutomaticallyAsync(Project project, string currentUserId, DateTime now)
        {
            var oldProjectStatus = project.Status;
            if (await _projectDomainService.TryStartAcceptanceAsync(project, currentUserId, now))
            {
                var summary = await _projectQuery.FindAcceptanceSummary(project.Id);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_ACCEPTANCE_START, AuditLogConstants.ACTION_TYPE_UPDATE, $"Tự động bắt đầu nghiệm thu dự án {project.Name}",
                    new { ProjectId = project.Id, Status = oldProjectStatus },
                    new { ProjectId = project.Id, project.Status, IsForced = false, IsAutomatic = true, Reason = (string?)null, Summary = summary.Total });
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private record ProjectSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, List<MappedImportColumn> MappedColumns);

        private record OverwriteSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, ProjectDocumentTypeDocumentField Mapping,
            int ColumnImportIndex);

        private record ImportAuditSource(string Description, string? SheetUrl, string? SheetName);
    }
}
