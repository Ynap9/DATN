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
using sh.be.domain.Module.Construction.QaAssignments.Services;
using sh.be.domain.Module.Construction.Submissions.Repositories;
using sh.be.domain.Module.Construction.Submissions.Services;
using sh.be.domain.Module.Construction.Submissions.Services.Check;
using sh.be.domain.Module.Construction.Submissions.Services.Check.Models;
using sh.be.domain.Module.Construction.Submissions.Services.Import;
using sh.be.domain.Module.Construction.Submissions.Services.Import.Models;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionService : BaseService, ISubmissionService
    {
        private readonly ISubmissionQuery _submissionQuery;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly QaAssignmentDomainService _qaAssignmentDomainService;
        private readonly IDataReaderExternal _dataReaderExternal;
        private readonly IExcelExternal _excelExternal;
        private readonly IBackgroundJobExternal _backgroundJobExternal;
        private readonly IFileStorageExternal _fileStorageExternal;

        public SubmissionService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISubmissionQuery submissionQuery, ISubmissionRepository submissionRepository, IRoleRepository roleRepository, IUserRepository userRepository,
            SubmissionDomainService submissionDomainService, QaAssignmentDomainService qaAssignmentDomainService, IDataReaderExternal dataReaderExternal, IExcelExternal excelExternal,
            IBackgroundJobExternal backgroundJobExternal, IFileStorageExternal fileStorageExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionQuery = submissionQuery;
            _submissionRepository = submissionRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _submissionDomainService = submissionDomainService;
            _qaAssignmentDomainService = qaAssignmentDomainService;
            _dataReaderExternal = dataReaderExternal;
            _excelExternal = excelExternal;
            _backgroundJobExternal = backgroundJobExternal;
            _fileStorageExternal = fileStorageExternal;
        }

        public async Task<ViewVerifyImportAppendSubmissionDto> VerifyImportAppend(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportAppend)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadAppendSheetAsync(dto);
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

        public async Task<ViewImportSubmissionResultDto> ImportAppend(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportAppend)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadAppendSheetAsync(dto);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportAppend),
                () => BuildAppendPlanAsync(dto, source, currentUserId, now),
                async appendPlan =>
                {
                    if (!appendPlan.HasChanges)
                    {
                        return;
                    }

                    await _submissionRepository.BulkInsertSubmissionsAsync(appendPlan.NewSubmissions.Select(draft => draft.Submission).ToList());
                    await _submissionRepository.BulkInsertDatasAsync(appendPlan.NewSubmissions.SelectMany(draft => draft.AssignDatasToSubmission()).ToList());
                    AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_APPEND, AuditLogConstants.ACTION_TYPE_CREATE,
                        $"Import thêm mới {appendPlan.NewSubmissions.Count} hồ sơ, {appendPlan.NewDataCount} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}",
                        null, new
                        {
                            dto.ProjectId,
                            dto.DocumentTypeId,
                            SheetUrl = source.Project.UrlGoogleSheetSync,
                            SheetName = source.Project.GoogleSheetName,
                            NewSubmissionCount = appendPlan.NewSubmissions.Count,
                            appendPlan.NewDataCount,
                            appendPlan.MappedFieldCount,
                        });
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Import hoàn tất. Hồ sơ thêm mới: {plan.NewSubmissions.Count}, "
                    + $"Dữ liệu thêm mới: {plan.NewDataCount}, Trường đã map: {plan.MappedFieldCount}, Ô dữ liệu có giá trị: {plan.NonEmptyCellCount}",
                TotalDataFields = plan.NonEmptyCellCount,
            };
        }

        public async Task<ViewVerifyImportBlankSubmissionDto> VerifyImportBlank(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportBlank)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadProjectSheetAsync(dto, null);
            var plan = await BuildBlankPlanAsync(dto, source, getCurrentUserId(), GetVietnamTime());

            return new ViewVerifyImportBlankSubmissionDto
            {
                TotalBlankCount = plan.ChangedDataCount,
                TotalFieldCount = plan.MappedFieldCount,
            };
        }

        public async Task<ViewImportSubmissionResultDto> ImportBlank(ImportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportBlank)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadProjectSheetAsync(dto, null);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportBlank),
                () => BuildBlankPlanAsync(dto, source, currentUserId, now),
                async blankPlan =>
                {
                    if (!blankPlan.HasChanges)
                    {
                        return;
                    }

                    await _submissionRepository.BulkUpdateDatasAsync(blankPlan.DatasToUpdate.ToList());
                    await _submissionRepository.BulkInsertDatasAsync(blankPlan.DatasToInsert.ToList());
                    AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_BLANK, AuditLogConstants.ACTION_TYPE_UPDATE,
                        $"Bổ sung {blankPlan.DatasToUpdate.Count} ô trống, thêm {blankPlan.DatasToInsert.Count} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}",
                        null, new
                        {
                            dto.ProjectId,
                            dto.DocumentTypeId,
                            SheetUrl = source.Project.UrlGoogleSheetSync,
                            SheetName = source.Project.GoogleSheetName,
                            UpdatedDataCount = blankPlan.DatasToUpdate.Count,
                            NewDataCount = blankPlan.DatasToInsert.Count,
                            blankPlan.MappedFieldCount,
                        });
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Import bổ sung hoàn tất. Ô trống đã điền: {plan.DatasToUpdate.Count}, "
                    + $"Dữ liệu thêm mới: {plan.DatasToInsert.Count}",
                TotalDataFields = plan.ChangedDataCount,
            };
        }

        public async Task<ViewVerifyImportOverwriteSubmissionDto> VerifyImportOverwrite(ImportOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(VerifyImportOverwrite)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadOverwriteSheetAsync(dto);
            var plan = await BuildOverwritePlanAsync(dto, source, getCurrentUserId(), GetVietnamTime());

            return new ViewVerifyImportOverwriteSubmissionDto
            {
                TotalOverWriteCount = plan.ChangedDataCount,
                TotalSkippedChecked = plan.SkippedCheckedCount,
            };
        }

        public async Task<ViewImportSubmissionResultDto> ImportOverwrite(ImportOverwriteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ImportOverwrite)}, dto={JsonSerializer.Serialize(dto)}");
            var source = await LoadOverwriteSheetAsync(dto);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportOverwrite),
                () => BuildOverwritePlanAsync(dto, source, currentUserId, now),
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
                            + $"loại văn bản Id = {dto.DocumentTypeId} từ sheet {dto.SheetName}",
                        overwritePlan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.OldData }),
                        overwritePlan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.NewData }));
                });

            return new ViewImportSubmissionResultDto
            {
                Message = $"Ghi đè hoàn tất. Đã ghi đè: {plan.DatasToUpdate.Count}, "
                    + $"Thêm mới: {plan.DatasToInsert.Count}, Bỏ qua do đã kiểm tra: {plan.SkippedCheckedCount}",
                TotalDataFields = plan.ChangedDataCount,
            };
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            return await _submissionQuery.FindPaging(dto);
        }

        public async Task<ViewSubmissionDetailDto> FindById(int id, FindSubmissionDetailDto dto)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var detail = await _submissionQuery.FindById(id, dto, dto.IncludePosition)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            if (detail.ProjectId != dto.ProjectId || detail.DocumentTypeId != dto.DocumentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
            return detail;
        }

        public async Task<ViewSubmissionDetailDto> FindNext(int id, SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(FindNext)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var nextId = await _submissionQuery.FindNextId(id, dto)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange);
            return await _submissionQuery.FindById(nextId, dto, false)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        public async Task<ViewSubmissionDetailDto> FindPrevious(int id, SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(FindPrevious)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var previousId = await _submissionQuery.FindPreviousId(id, dto)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionPositionOutOfRange);
            return await _submissionQuery.FindById(previousId, dto, false)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
        }

        public async Task<ViewCountSubmissionDto> Count(SubmissionFilterDto dto)
        {
            _logger.LogInformation($"{nameof(Count)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            return await _submissionQuery.Count(dto);
        }

        public async Task<ViewCheckSubmissionResultDto> Check(CheckSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(Check)}, dto={JsonSerializer.Serialize(dto)}");
            var submission = await _submissionDomainService.EnsureSubmissionExistsAsync(dto.Id);
            submission.EnsureBelongsTo(dto.ProjectId, dto.DocumentTypeId);

            var currentUserId = getCurrentUserId();
            var isSuperAdmin = PermissionGrantPolicy.IsSuperAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, isSuperAdmin);
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var existingDatas = await _submissionRepository.GetDatasBySubmissionIdAsync(dto.Id);
            var userName = (await _userRepository.FindByIdAsync(currentUserId))?.UserName ?? currentUserId;

            var now = GetVietnamTime();
            var inputs = dto.Datas.Select(data => new SubmissionDataInput(data.DocumentFieldId, data.Data)).ToList();
            var plan = SubmissionCheckPolicy.BuildPlan(submission, inputs, mappings, existingDatas, currentUserId, now);
            var log = submission.Check(currentUserId, userName, dto.Note, plan.ChangedDataCount, now);

            _submissionRepository.AddDatas(plan.DatasToInsert);
            _submissionRepository.AddLog(log);
            AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_CHECK, AuditLogConstants.ACTION_TYPE_UPDATE,
                $"Kiểm tra hồ sơ {submission.FileName} (Id = {dto.Id}) của dự án {project.Name}, sửa {plan.ChangedDataCount} trường",
                new { StatusCheck = log.OldStatus, Datas = plan.Changes.Select(change => new { change.DocumentFieldId, Data = change.OldData }) },
                new { StatusCheck = log.NewStatus, Datas = plan.Changes.Select(change => new { change.DocumentFieldId, Data = change.NewData }), log.Note });

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Check)}, id={dto.Id}");
                throw;
            }

            return new ViewCheckSubmissionResultDto
            {
                SubmissionId = dto.Id,
                StatusCheck = submission.StatusCheck,
                ChangedDataCount = plan.ChangedDataCount,
                LogId = log.Id,
            };
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int id, FindPagingSubmissionLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindLogPaging)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureSubmissionExistsAsync(id);
            return await _submissionQuery.FindLogPaging(id, dto);
        }

        public async Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > SubmissionConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {SubmissionConst.MAX_PAGE_SIZE}");
            }
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            return await _submissionQuery.FindQaPaging(dto);
        }

        public async Task<ViewQaSubmissionDetailDto> FindQaById(int id, FindQaSubmissionDetailDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaById)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var detail = await _submissionQuery.FindQaById(id, dto, dto.IncludePosition)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotFound);
            if (detail.ProjectId != dto.ProjectId || detail.DocumentTypeId != dto.DocumentTypeId)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorSubmissionNotInDocumentType);
            }
            return detail;
        }

        public async Task<ViewQaResultDto> ReviewQa(ReviewQaSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ReviewQa)}, dto={JsonSerializer.Serialize(dto)}");
            var submission = await _submissionDomainService.EnsureSubmissionExistsAsync(dto.Id);
            submission.EnsureBelongsTo(dto.ProjectId, dto.DocumentTypeId);

            var currentUserId = getCurrentUserId();
            var isSuperAdmin = PermissionGrantPolicy.IsSuperAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, isSuperAdmin);
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

                await transaction.CommitAsync();

                return new ViewQaResultDto
                {
                    SubmissionId = dto.Id,
                    StatusCheck = submission.StatusCheck,
                    StatusQa = submission.StatusQa,
                    QaReason = submission.QaReason,
                    WrongDocumentFieldIds = wrongFields.Select(field => field.DocumentFieldId).ToList(),
                    QaLogId = qaLog.Id,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(ReviewQa)}, id={dto.Id}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int id, FindPagingSubmissionQaLogDto dto)
        {
            _logger.LogInformation($"{nameof(FindQaLogPaging)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureSubmissionExistsAsync(id);
            return await _submissionQuery.FindQaLogPaging(id, dto);
        }

        public async Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto)
        {
            _logger.LogInformation($"{nameof(FindFieldValues)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            await _submissionDomainService.EnsureDocumentFieldInProjectAsync(dto.ProjectId, dto.DocumentTypeId, dto.DocumentFieldId);
            return await _submissionQuery.FindFieldValues(dto);
        }

        public async Task<ViewSubmissionTreeDto> FindTree(FindSubmissionTreeDto dto)
        {
            _logger.LogInformation($"{nameof(FindTree)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            return await _submissionQuery.FindTree(dto);
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

        public async Task<List<GetListSubmissionTimelineDto>> FindTimeline(int id)
        {
            _logger.LogInformation($"{nameof(FindTimeline)}, id={id}");
            await _submissionDomainService.EnsureSubmissionExistsAsync(id);
            return await _submissionQuery.FindTimeline(id);
        }

        public async Task<ViewDeleteSubmissionResultDto> Delete(DeleteSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(Delete)}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var isSuperAdmin = PermissionGrantPolicy.IsSuperAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var project = await _submissionDomainService.EnsureEditableProjectAsync(dto.ProjectId, isSuperAdmin);
            var now = GetVietnamTime();

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _submissionDomainService.LockImportAsync(dto.ProjectId, dto.DocumentTypeId);
                var submissions = await _submissionDomainService.EnsureSubmissionsDeletableAsync(dto.ProjectId, dto.DocumentTypeId, dto.Ids, isSuperAdmin);
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

        public async Task<ExportFileDto> ExportExcel(ExportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(ExportExcel)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
            var table = await _submissionQuery.FindExportTable(dto);
            return _excelExternal.CreateTable($"{table.ProjectName}_{table.DocumentTypeName}_{GetVietnamTime():yyyyMMddHHmm}", table.DocumentTypeName,
                table.Headers, table.Rows);
        }

        public async Task<ViewSubmissionExportJobDto> CreateExportJob(ExportSubmissionDto dto)
        {
            _logger.LogInformation($"{nameof(CreateExportJob)}, dto={JsonSerializer.Serialize(dto)}");
            await _submissionDomainService.EnsureReadableDocumentTypeAsync(dto.ProjectId, dto.DocumentTypeId);
            await EnsureQaTicketFilterVisibleAsync(dto);
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

        private async Task<ProjectSheetSource> LoadAppendSheetAsync(ImportSubmissionDto dto)
        {
            var source = await LoadProjectSheetAsync(dto, dto.LinkColumnImport);
            SubmissionImportPolicy.EnsureFileUrlsPresent(source.Table, source.Columns);
            return source;
        }

        private async Task<ProjectSheetSource> LoadProjectSheetAsync(ImportSubmissionDto dto, string? fileUrlColumn)
        {
            var project = await _submissionDomainService.EnsureImportableProjectAsync(dto.ProjectId);
            await _submissionDomainService.EnsureDocumentTypeInProjectAsync(dto.ProjectId, dto.DocumentTypeId);
            var tableTask = _dataReaderExternal.ReadTableAsync(project.UrlGoogleSheetSync, project.GoogleSheetName);
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var table = await tableTask;

            var columns = ResolveSheetColumns(table, dto, fileUrlColumn);
            var mappedColumns = SubmissionImportColumnPolicy.ResolveMappedColumns(table.Headers, mappings);
            return new ProjectSheetSource(project, table, columns, mappedColumns);
        }

        private async Task<OverwriteSheetSource> LoadOverwriteSheetAsync(ImportOverwriteSubmissionDto dto)
        {
            var tableTask = _dataReaderExternal.ReadTableAsync(dto.Url, dto.SheetName);
            var project = await _submissionDomainService.EnsureImportableProjectAsync(dto.ProjectId);
            await _submissionDomainService.EnsureDocumentTypeInProjectAsync(dto.ProjectId, dto.DocumentTypeId);
            var mapping = await _submissionDomainService.EnsureDocumentFieldInProjectAsync(dto.ProjectId, dto.DocumentTypeId, dto.DocumentFieldId);
            var table = await tableTask;

            var columns = ResolveSheetColumns(table, dto, null);
            var columnImportIndex = SubmissionImportColumnPolicy.ResolveSingleColumn(table.Headers, dto.ColumnImport);
            return new OverwriteSheetSource(project, table, columns, mapping, columnImportIndex);
        }

        private static SubmissionImportColumns ResolveSheetColumns(SheetTable table, ImportSubmissionDto dto, string? fileUrlColumn)
        {
            SubmissionImportPolicy.EnsureHasData(table);
            var columns = SubmissionImportColumnPolicy.ResolveColumns(table.Headers, dto.FileColumnImport, dto.ProfileRange, dto.Box, fileUrlColumn);
            SubmissionImportPolicy.EnsureNoDuplicateKeys(table, columns);
            return columns;
        }

        private async Task<AppendImportPlan> BuildAppendPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, string? importedBy, DateTime importedDate)
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            return SubmissionImportPolicy.BuildAppendPlan(source.Table, source.Columns, source.MappedColumns, snapshots,
                dto.ProjectId, dto.DocumentTypeId, importedBy, importedDate);
        }

        private async Task<BlankImportPlan> BuildBlankPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, string? importedBy, DateTime importedDate)
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            var documentFieldIds = source.MappedColumns.Select(column => column.DocumentFieldId).ToList();
            var existingDatas = await _submissionRepository.GetDatasAsync(dto.ProjectId, dto.DocumentTypeId, documentFieldIds);
            return SubmissionImportPolicy.BuildBlankPlan(source.Table, source.Columns, source.MappedColumns, snapshots, existingDatas,
                dto.ProjectId, dto.DocumentTypeId, importedBy, importedDate);
        }

        private async Task<OverwriteImportPlan> BuildOverwritePlanAsync(ImportOverwriteSubmissionDto dto, OverwriteSheetSource source,
            string? importedBy, DateTime importedDate)
        {
            var snapshots = await _submissionRepository.GetSnapshotsAsync(dto.ProjectId, dto.DocumentTypeId);
            var existingDatas = await _submissionRepository.GetDatasAsync(dto.ProjectId, dto.DocumentTypeId, [dto.DocumentFieldId]);
            return SubmissionImportPolicy.BuildOverwritePlan(source.Table, source.Columns, source.ColumnImportIndex, source.Mapping, snapshots,
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

        private record ProjectSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, List<MappedImportColumn> MappedColumns);

        private record OverwriteSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, ProjectDocumentTypeDocumentField Mapping,
            int ColumnImportIndex);
    }
}
