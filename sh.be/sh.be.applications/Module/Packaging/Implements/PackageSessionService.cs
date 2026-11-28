using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.Packaging.Dtos.PackageSession;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.applications.Module.Packaging.Jobs;
using sh.be.domain.Base;
using sh.be.domain.Base.Models;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Packaging.Entities;
using sh.be.domain.Module.Packaging.Repositories;
using sh.be.domain.Module.Packaging.Services;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Packaging.Implements
{
    public class PackageSessionService : BaseService, IPackageSessionService
    {
        private readonly IPackageSessionRepository _packageSessionRepository;
        private readonly IPackageSessionQuery _packageSessionQuery;
        private readonly IPackageConfigQuery _packageConfigQuery;
        private readonly IPackageFileInspectExternal _packageFileInspectExternal;
        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly IExcelReaderExternal _excelReaderExternal;
        private readonly IBackgroundJobExternal _backgroundJobExternal;

        public PackageSessionService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PackageSessionService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPackageSessionRepository packageSessionRepository, IPackageSessionQuery packageSessionQuery, IPackageConfigQuery packageConfigQuery,
            IPackageFileInspectExternal packageFileInspectExternal, IFileStorageExternal fileStorageExternal, IExcelReaderExternal excelReaderExternal,
            IBackgroundJobExternal backgroundJobExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _packageSessionRepository = packageSessionRepository;
            _packageSessionQuery = packageSessionQuery;
            _packageConfigQuery = packageConfigQuery;
            _packageFileInspectExternal = packageFileInspectExternal;
            _fileStorageExternal = fileStorageExternal;
            _excelReaderExternal = excelReaderExternal;
            _backgroundJobExternal = backgroundJobExternal;
        }

        public async Task<ViewCreatePackageSessionDto> Create(CreatePackageSessionDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            PackageSchemaDomainService.EnsureValidPackageType(dto.PackageType);
            PackageSessionDomainService.EnsureValidSessionMetadataTypes(dto.MetadataTypes);

            var session = PackageSession.Create(dto.PackageType, dto.MetadataTypes, getCurrentUserId(), GetVietnamTime());
            _packageSessionRepository.Add(session);
            AddAuditLog(AuditLogConstants.ACTION_PACKAGE_SESSION_CREATE, AuditLogConstants.ACTION_TYPE_CREATE,
                $"Tạo phiên đóng gói {PackageConst.PACKAGE_TYPE_CODES[dto.PackageType]}", null, new { session.PackageType, session.MetadataTypes });
            await _unitOfWork.SaveChangesAsync();

            return new ViewCreatePackageSessionDto
            {
                Id = session.Id,
                Sheets = await _packageConfigQuery.GetTemplateSheets(session.PackageType, session.GetMetadataTypes()),
            };
        }

        // Nhận một đợt file của phiên đang chờ: soi từng file, đẩy MinIO với khoá Guid (2 đợt song song không trùng khoá), lưu danh sách file
        public async Task<ViewAddFilesPackageSessionDto> AddFiles(int id, AddFilesPackageSessionDto dto)
        {
            _logger.LogInformation($"{nameof(AddFiles)}, id={id}, fileCount={dto.Files.Count}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var session = await FindSessionAsync(id);
            var relativePaths = PackageSessionDomainService.NormalizeUploadPaths(dto.Files.Count, dto.RelativePaths);
            session.AcceptUpload(relativePaths[0].Split('/')[0], currentUserId, now);
            var metadataTypes = session.GetMetadataTypes();
            var checks = dto.Files
                .Select((file, index) => (File: file, Check: PackageSessionDomainService.CheckUploadFile(session.PackageType, metadataTypes, relativePaths[index], file.Length)))
                .ToList();

            var uploads = checks
                .Where(c => c.Check.IsAccepted)
                .Select(c => (c.File, c.Check, ObjectKey: PackageSessionDomainService.BuildSourceObjectKey(id, c.Check.Extension)))
                .ToList();

            var documents = new PackageDocument[uploads.Count];
            await Parallel.ForEachAsync(Enumerable.Range(0, uploads.Count), new ParallelOptions { MaxDegreeOfParallelism = PackageSessionConst.UPLOAD_PARALLELISM },
                async (index, _) =>
                {
                    var upload = uploads[index];
                    var mimeType = PackageSessionConst.MIME_TYPES[upload.Check.Extension];
                    PackageFileInspectDto inspect;
                    await using (var content = upload.File.OpenReadStream())
                    {
                        inspect = await _packageFileInspectExternal.InspectAsync(content, upload.Check.Extension == PackageSessionConst.PDF_EXTENSION);
                    }
                    await using (var content = upload.File.OpenReadStream())
                    {
                        await _fileStorageExternal.UploadPrivateAsync(content, upload.ObjectKey, mimeType);
                    }
                    documents[index] = PackageDocument.Create(id, upload.Check.FileRole!, upload.Check.RelativePath, upload.Check.FileName, upload.Check.Extension, mimeType,
                        upload.ObjectKey, upload.File.Length, inspect.Sha256, inspect.IsSigned, inspect.PdfAPart, inspect.PageCount, currentUserId, now);
                });

            _packageSessionRepository.AddDocuments(documents.ToList());
            _packageSessionRepository.Update(session);
            AddAuditLog(AuditLogConstants.ACTION_PACKAGE_SESSION_ADD_FILES, AuditLogConstants.ACTION_TYPE_CREATE, $"Tải {uploads.Count} file lên phiên đóng gói {id}",
                null, new { session.Id, AddedFiles = uploads.Count });
            await _unitOfWork.SaveChangesAsync();

            return new ViewAddFilesPackageSessionDto
            {
                AcceptedCount = uploads.Count,
                Skipped = checks
                    .Where(c => c.Check.IsSkipped)
                    .Select(c => new ViewPackageUploadIssueDto { RelativePath = c.Check.RelativePath, Reason = c.Check.Reason })
                    .ToList(),
                Rejected = checks
                    .Where(c => !c.Check.IsAccepted && !c.Check.IsSkipped)
                    .Select(c => new ViewPackageUploadIssueDto { RelativePath = c.Check.RelativePath, Reason = c.Check.Reason })
                    .ToList(),
            };
        }

        public async Task<PackageSessionProgressDto> Start(int id)
        {
            _logger.LogInformation($"{nameof(Start)}, id={id}");
            var currentUserId = getCurrentUserId();
            var session = await FindSessionAsync(id);
            var excelCount = await _packageSessionRepository.CountDocumentsByRoleAsync(id, PackageSessionConst.FILE_ROLE_EXCEL);
            var documentCount = await _packageSessionRepository.CountDocumentsByRoleAsync(id, PackageSessionConst.FILE_ROLE_DATA);
            session.Start(excelCount, documentCount, currentUserId, GetVietnamTime());
            if (!_backgroundJobExternal.HasServerForQueue(BackgroundJobConst.QUEUE_DEFAULT))
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorBackgroundJobUnavailable);
            }

            _packageSessionRepository.Update(session);
            AddAuditLog(AuditLogConstants.ACTION_PACKAGE_SESSION_START, AuditLogConstants.ACTION_TYPE_UPDATE, $"Bắt đầu kiểm phiên đóng gói {id}",
                null, new { session.Id, session.TotalDocument });
            await _unitOfWork.SaveChangesAsync();

            _backgroundJobExternal.Enqueue<PackageVerifyJob>(job => job.ExecuteAsync(id, currentUserId), BackgroundJobConst.QUEUE_DEFAULT,
                new Dictionary<string, string>
                {
                    [BackgroundJobConst.PARAMETER_JOB_TYPE] = PackageSessionConst.VERIFY_JOB_TYPE,
                    [BackgroundJobConst.PARAMETER_USER_ID] = currentUserId,
                });
            return await GetProgress(id);
        }

        public async Task<PackageSessionProgressDto> GetProgress(int id)
        {
            return await _packageSessionQuery.FindProgress(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
        }

        public async Task<ViewPackageSessionResultDto> GetResult(int id)
        {
            _logger.LogInformation($"{nameof(GetResult)}, id={id}");
            var result = await _packageSessionQuery.FindResult(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
            var matchReport = await _packageSessionQuery.FindMatchReport(id);
            result.Excels = matchReport == null ? new() : JsonSerializer.Deserialize<List<ViewPackageExcelReportDto>>(matchReport) ?? new();
            return result;
        }

        public async Task<BaseResponsePagingDto<GetListPackageDocumentDto>> FindDocumentPaging(int id, FindPagingPackageDocumentDto dto)
        {
            _logger.LogInformation($"{nameof(FindDocumentPaging)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            return await _packageSessionQuery.FindDocumentPaging(id, dto);
        }

        public async Task<PackageSessionProgressDto> Cancel(int id)
        {
            _logger.LogInformation($"{nameof(Cancel)}, id={id}");
            var session = await FindSessionAsync(id);
            var oldData = new { session.Id, session.Status };
            session.Cancel(getCurrentUserId(), GetVietnamTime());

            _packageSessionRepository.Update(session);
            AddAuditLog(AuditLogConstants.ACTION_PACKAGE_SESSION_CANCEL, AuditLogConstants.ACTION_TYPE_UPDATE, $"Hủy phiên đóng gói {id}",
                oldData, new { session.Id, session.Status });
            await _unitOfWork.SaveChangesAsync();

            await _fileStorageExternal.DeleteByPrefixAsync(PackageSessionDomainService.BuildSessionObjectKeyPrefix(id));
            return await GetProgress(id);
        }

        // Job kiểm phiên: đọc Excel, ghép file với dòng metadata, ghi kết quả theo lô; dừng khi phiên bị huỷ giữa chừng
        public async Task VerifyFromJob(int id, string userId)
        {
            _logger.LogInformation($"{nameof(VerifyFromJob)}, id={id}, userId={userId}");
            var session = await FindSessionAsync(id);
            if (session.Status != PackageSessionConst.RUNNING)
            {
                return;
            }

            try
            {
                var config = await _packageConfigQuery.GetVerifyConfig(session.PackageType, session.GetMetadataTypes());
                var documents = await _packageSessionRepository.GetDocumentsAsync(id);
                var workbooks = new List<PackageExcelWorkbook>();
                foreach (var excel in documents.Where(d => d.FileRole == PackageSessionConst.FILE_ROLE_EXCEL))
                {
                    Dictionary<string, SheetTable>? sheets = new(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        var sheetNames = (await _excelReaderExternal.GetSheetHeadersAsync(excel.ObjectKey))
                            .ToDictionary(s => s.SheetName, s => s.SheetName, StringComparer.OrdinalIgnoreCase);
                        foreach (var schema in config.Schemas.Where(s => sheetNames.ContainsKey(s.Name)))
                        {
                            sheets[schema.Name] = await _excelReaderExternal.ReadTableAsync(excel.ObjectKey, sheetNames[schema.Name]);
                        }
                    }
                    catch (UserFriendlyException ex)
                    {
                        _logger.LogWarning(ex, $"{nameof(VerifyFromJob)}, excelObjectKey={excel.ObjectKey}");
                        sheets = null;
                    }
                    workbooks.Add(new PackageExcelWorkbook(excel, sheets));
                }

                var plan = PackageSessionDomainService.BuildVerifyPlan(session, config, documents, workbooks, userId, GetVietnamTime());
                _packageSessionRepository.AddDossiers(plan.Dossiers.Select(d => d.Dossier).ToList());
                session.SaveMatchReport(JsonSerializer.Serialize(plan.ExcelReports));
                await _unitOfWork.SaveChangesAsync();

                foreach (var batch in plan.Documents.OrderBy(c => c.FileRole == PackageSessionConst.FILE_ROLE_EXCEL ? 0 : 1).Chunk(PackageSessionConst.VERIFY_BATCH_SIZE))
                {
                    if (await _packageSessionRepository.GetStatusAsync(id) != PackageSessionConst.RUNNING)
                    {
                        return;
                    }
                    var now = GetVietnamTime();
                    foreach (var check in batch)
                    {
                        check.Document.Complete(check.Dossier?.Id, check.FileRole, check.MetadataType, check.DocKey, check.Errors, check.Warnings, userId, now);
                    }
                    var doneDocuments = batch.Where(c => c.FileRole != PackageSessionConst.FILE_ROLE_EXCEL).ToList();
                    session.AddProgress(doneDocuments.Count, doneDocuments.Count(c => c.Errors.Count > 0),
                        doneDocuments.Count(c => c.Errors.Count == 0 && c.Warnings.Count > 0), userId, now);
                    await _unitOfWork.SaveChangesAsync();
                }

                if (await _packageSessionRepository.GetStatusAsync(id) != PackageSessionConst.RUNNING)
                {
                    return;
                }
                var finishedDate = GetVietnamTime();
                foreach (var dossierCheck in plan.Dossiers)
                {
                    dossierCheck.Dossier.Complete(dossierCheck.Errors, dossierCheck.Warnings,
                        plan.Documents.Where(c => c.Dossier == dossierCheck.Dossier).Select(c => c.Document).ToList(), userId, finishedDate);
                }
                session.Finish(plan.Dossiers.Select(d => d.Dossier).ToList(), plan.HasErrorOutsideDossier, userId, finishedDate);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(VerifyFromJob)}, id={id}");
                // Phiên bị huỷ trong lúc job chạy thì giữ cancelled, không ghi đè thành failed
                if (await _packageSessionRepository.GetStatusAsync(id) != PackageSessionConst.RUNNING)
                {
                    return;
                }
                session.Fail(ex is UserFriendlyException ? ex.Message : PackageSessionConst.VERIFY_FAILED_MESSAGE, userId, GetVietnamTime());
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task<PackageSession> FindSessionAsync(int id)
        {
            return await _packageSessionRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
        }
    }
}
