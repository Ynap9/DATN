using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.applications.Module.Construction.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Base.Models;
using sh.be.domain.Module.Construction.Entities;
using sh.be.domain.Module.Construction.Repositories;
using sh.be.domain.Module.Construction.Services;
using sh.be.domain.Module.Construction.Services.Import;
using sh.be.domain.Module.Construction.Services.Import.Models;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using System.Diagnostics;
using System.Text.Json;

namespace sh.be.applications.Module.Construction.Implements
{
    public class SubmissionImportService : BaseService, ISubmissionImportService
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly SubmissionDomainService _submissionDomainService;
        private readonly IDataReader _dataReader;

        public SubmissionImportService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SubmissionImportService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ISubmissionRepository submissionRepository, SubmissionDomainService submissionDomainService, IDataReader dataReader
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _submissionRepository = submissionRepository;
            _submissionDomainService = submissionDomainService;
            _dataReader = dataReader;
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
            var stopwatch = Stopwatch.StartNew();
            var source = await LoadAppendSheetAsync(dto);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportAppend),
                () => BuildAppendPlanAsync(dto, source, currentUserId, now),
                appendPlan => SaveAppendPlanAsync(dto, source, appendPlan));

            var durationSeconds = stopwatch.Elapsed.TotalSeconds;
            return new ViewImportSubmissionResultDto
            {
                Message = $"Import hoàn tất sau {durationSeconds:0.00} giây. Hồ sơ thêm mới: {plan.NewSubmissions.Count}, "
                    + $"Dữ liệu thêm mới: {plan.NewDataCount}, Trường đã map: {plan.MappedFieldCount}, Ô dữ liệu có giá trị: {plan.NonEmptyCellCount}",
                TotalDataFields = plan.NonEmptyCellCount,
                DurationSeconds = durationSeconds,
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
            var stopwatch = Stopwatch.StartNew();
            var source = await LoadProjectSheetAsync(dto, null);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportBlank),
                () => BuildBlankPlanAsync(dto, source, currentUserId, now),
                blankPlan => SaveBlankPlanAsync(dto, source, blankPlan));

            var durationSeconds = stopwatch.Elapsed.TotalSeconds;
            return new ViewImportSubmissionResultDto
            {
                Message = $"Import bổ sung hoàn tất sau {durationSeconds:0.00} giây. Ô trống đã điền: {plan.DatasToUpdate.Count}, "
                    + $"Dữ liệu thêm mới: {plan.DatasToInsert.Count}",
                TotalDataFields = plan.ChangedDataCount,
                DurationSeconds = durationSeconds,
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
            var stopwatch = Stopwatch.StartNew();
            var source = await LoadOverwriteSheetAsync(dto);
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var plan = await ImportInTransactionAsync(dto, nameof(ImportOverwrite),
                () => BuildOverwritePlanAsync(dto, source, currentUserId, now),
                overwritePlan => SaveOverwritePlanAsync(dto, source, overwritePlan));

            var durationSeconds = stopwatch.Elapsed.TotalSeconds;
            return new ViewImportSubmissionResultDto
            {
                Message = $"Ghi đè hoàn tất sau {durationSeconds:0.00} giây. Đã ghi đè: {plan.DatasToUpdate.Count}, "
                    + $"Thêm mới: {plan.DatasToInsert.Count}, Bỏ qua do đã kiểm tra: {plan.SkippedCheckedCount}",
                TotalDataFields = plan.ChangedDataCount,
                DurationSeconds = durationSeconds,
            };
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
            var tableTask = _dataReader.ReadTableAsync(project.UrlGoogleSheetSync, project.GoogleSheetName);
            var mappings = await _submissionDomainService.GetImportMappingsAsync(dto.ProjectId, dto.DocumentTypeId);
            var table = await tableTask;

            var columns = ResolveSheetColumns(table, dto, fileUrlColumn);
            var mappedColumns = SubmissionImportColumnPolicy.ResolveMappedColumns(table.Headers, mappings);
            return new ProjectSheetSource(project, table, columns, mappedColumns);
        }

        private async Task<OverwriteSheetSource> LoadOverwriteSheetAsync(ImportOverwriteSubmissionDto dto)
        {
            var tableTask = _dataReader.ReadTableAsync(dto.Url, dto.SheetName);
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

        private async Task SaveAppendPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, AppendImportPlan plan)
        {
            if (!plan.HasChanges)
            {
                return;
            }

            await _submissionRepository.BulkInsertSubmissionsAsync(plan.NewSubmissions.Select(draft => draft.Submission).ToList());
            await _submissionRepository.BulkInsertDatasAsync(plan.NewSubmissions.SelectMany(draft => draft.AssignDatasToSubmission()).ToList());
            AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_APPEND, AuditLogConstants.ACTION_TYPE_CREATE,
                $"Import thêm mới {plan.NewSubmissions.Count} hồ sơ, {plan.NewDataCount} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}",
                null, new
                {
                    dto.ProjectId,
                    dto.DocumentTypeId,
                    SheetUrl = source.Project.UrlGoogleSheetSync,
                    SheetName = source.Project.GoogleSheetName,
                    NewSubmissionCount = plan.NewSubmissions.Count,
                    plan.NewDataCount,
                    plan.MappedFieldCount,
                });
        }

        private async Task SaveBlankPlanAsync(ImportSubmissionDto dto, ProjectSheetSource source, BlankImportPlan plan)
        {
            if (!plan.HasChanges)
            {
                return;
            }

            await _submissionRepository.BulkUpdateDatasAsync(plan.DatasToUpdate.ToList());
            await _submissionRepository.BulkInsertDatasAsync(plan.DatasToInsert.ToList());
            AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_BLANK, AuditLogConstants.ACTION_TYPE_UPDATE,
                $"Bổ sung {plan.DatasToUpdate.Count} ô trống, thêm {plan.DatasToInsert.Count} dữ liệu vào dự án {source.Project.Name}, loại văn bản Id = {dto.DocumentTypeId}",
                null, new
                {
                    dto.ProjectId,
                    dto.DocumentTypeId,
                    SheetUrl = source.Project.UrlGoogleSheetSync,
                    SheetName = source.Project.GoogleSheetName,
                    UpdatedDataCount = plan.DatasToUpdate.Count,
                    NewDataCount = plan.DatasToInsert.Count,
                    plan.MappedFieldCount,
                });
        }

        private async Task SaveOverwritePlanAsync(ImportOverwriteSubmissionDto dto, OverwriteSheetSource source, OverwriteImportPlan plan)
        {
            if (!plan.HasChanges)
            {
                return;
            }

            await _submissionRepository.BulkUpdateDatasAsync(plan.DatasToUpdate.ToList());
            await _submissionRepository.BulkInsertDatasAsync(plan.DatasToInsert.ToList());
            AddAuditLog(AuditLogConstants.ACTION_SUBMISSION_IMPORT_OVERWRITE, AuditLogConstants.ACTION_TYPE_UPDATE,
                $"Ghi đè {plan.DatasToUpdate.Count}, thêm {plan.DatasToInsert.Count} dữ liệu trường Id = {dto.DocumentFieldId} của dự án {source.Project.Name}, "
                    + $"loại văn bản Id = {dto.DocumentTypeId} từ sheet {dto.SheetName}",
                plan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.OldData }),
                plan.Changes.Select(change => new { change.SubmissionId, change.FileName, change.DocumentFieldId, Data = change.NewData }));
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

        private record ProjectSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, List<MappedImportColumn> MappedColumns);

        private record OverwriteSheetSource(Project Project, SheetTable Table, SubmissionImportColumns Columns, ProjectDocumentTypeDocumentField Mapping,
            int ColumnImportIndex);
    }
}
