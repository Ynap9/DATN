using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.Storage;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.BackgroundJob;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.Packaging.Dtos.PackageBuild;
using sh.be.applications.Module.Packaging.Interfaces.Queries;
using sh.be.applications.Module.Packaging.Interfaces.Services;
using sh.be.applications.Module.Packaging.Jobs;
using sh.be.domain.Base;
using sh.be.domain.Base.Models;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Packaging.Repositories;
using sh.be.domain.Module.Packaging.Services;
using sh.be.shared.Constants.BackgroundJob;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Packaging;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Packaging.Implements
{
    public class PackageBuildService : BaseService, IPackageBuildService
    {
        private readonly IPackageSessionRepository _packageSessionRepository;
        private readonly IPackageSessionQuery _packageSessionQuery;
        private readonly IPackageConfigQuery _packageConfigQuery;
        private readonly IUserRepository _userRepository;
        private readonly IPackageArchiveExternal _packageArchiveExternal;
        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly IExcelReaderExternal _excelReaderExternal;
        private readonly IBackgroundJobExternal _backgroundJobExternal;

        public PackageBuildService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PackageBuildService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPackageSessionRepository packageSessionRepository, IPackageSessionQuery packageSessionQuery, IPackageConfigQuery packageConfigQuery, IUserRepository userRepository,
            IPackageArchiveExternal packageArchiveExternal, IFileStorageExternal fileStorageExternal, IExcelReaderExternal excelReaderExternal, IBackgroundJobExternal backgroundJobExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _packageSessionRepository = packageSessionRepository;
            _packageSessionQuery = packageSessionQuery;
            _packageConfigQuery = packageConfigQuery;
            _userRepository = userRepository;
            _packageArchiveExternal = packageArchiveExternal;
            _fileStorageExternal = fileStorageExternal;
            _excelReaderExternal = excelReaderExternal;
            _backgroundJobExternal = backgroundJobExternal;
        }

        // Bắt đầu dựng gói cho mọi gói không lỗi của phiên đã kiểm: lưu mã phông + cơ quan lưu trữ, xoá ZIP lần trước rồi đẩy job dựng
        public async Task<PackageBuildProgressDto> Build(int id, BuildPackageSessionDto dto)
        {
            _logger.LogInformation($"{nameof(Build)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            var session = await _packageSessionRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
            var user = await _userRepository.FindByIdAsync(currentUserId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var dossiers = await _packageSessionRepository.GetDossiersAsync(id);
            session.StartBuild(dto.FondsCode, dto.ArchiveCode, dto.ArchiveName, dossiers.Count(d => d.Status != PackageSessionConst.FAILED),
                user.UserName!, currentUserId, now);
            if (!_backgroundJobExternal.HasServerForQueue(BackgroundJobConst.QUEUE_DEFAULT))
            {
                throw new UserFriendlyException(ErrorCodes.CoreErrorBackgroundJobUnavailable);
            }
            foreach (var dossier in dossiers)
            {
                dossier.ResetBuild(currentUserId, now);
            }

            _packageSessionRepository.Update(session);
            AddAuditLog(AuditLogConstants.ACTION_PACKAGE_SESSION_BUILD, AuditLogConstants.ACTION_TYPE_UPDATE, $"Dựng gói TT05 phiên đóng gói {id}",
                null, new { session.Id, session.FondsCode, session.ArchiveCode, session.ArchiveName, session.BuildTotal });
            await _unitOfWork.SaveChangesAsync();

            // Xoá ZIP cũ trước khi đẩy job, không thì có thể xoá nhầm ZIP job mới vừa tải lên
            await _fileStorageExternal.DeleteByPrefixAsync(PackageSessionDomainService.BuildPackagesObjectKeyPrefix(id));
            _backgroundJobExternal.Enqueue<PackageBuildJob>(job => job.ExecuteAsync(id, currentUserId), BackgroundJobConst.QUEUE_DEFAULT,
                new Dictionary<string, string>
                {
                    [BackgroundJobConst.PARAMETER_JOB_TYPE] = PackageBuildConst.BUILD_JOB_TYPE,
                    [BackgroundJobConst.PARAMETER_USER_ID] = currentUserId,
                });
            return await GetBuildProgress(id);
        }

        // Tiến độ dựng gói của phiên
        public async Task<PackageBuildProgressDto> GetBuildProgress(int id)
        {
            return await _packageSessionQuery.FindBuildProgress(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
        }

        // Danh sách gói đã dựng kèm link tải tạm 1 giờ
        public async Task<List<GetListPackageBuildDto>> GetPackages(int id)
        {
            _logger.LogInformation($"{nameof(GetPackages)}, id={id}");
            _ = await _packageSessionRepository.GetStatusAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
            var packages = await _packageSessionQuery.GetPackages(id);
            foreach (var package in packages)
            {
                package.DownloadUrl = _fileStorageExternal.GetTemporaryUrl(package.PackageObjectKey, package.PackageFileName!, TimeSpan.FromHours(PackageBuildConst.PACKAGE_LINK_HOURS));
            }
            return packages;
        }

        // Job dựng: mỗi gói không lỗi ra 1 ZIP; gói dựng lỗi chỉ ghi vào ErrorSummary của gói, không dừng các gói khác
        public async Task BuildFromJob(int id, string userId)
        {
            _logger.LogInformation($"{nameof(BuildFromJob)}, id={id}, userId={userId}");
            var session = await _packageSessionRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.PackagingErrorSessionNotFound);
            if (session.BuildStatus != PackageSessionConst.RUNNING)
            {
                return;
            }

            try
            {
                // Lấy đủ 4 loại metadata vì schemas/ luôn có đủ 8 file kể cả khi phiên chỉ chọn văn bản
                var config = await _packageConfigQuery.GetVerifyConfig(session.PackageType, PackageSchemaConst.METADATA_TYPES.ToList());
                var sessionSchemas = config.Schemas.Where(s => session.GetMetadataTypes().Contains(s.MetadataType)).ToList();
                var packageSchema = config.Schemas.First(s => s.MetadataType == PackageSchemaConst.METADATA_PACKAGE);
                var archiveSchemas = config.Schemas
                    .Select(s => new PackageArchiveSchemaDto
                    {
                        XsdFileName = s.XsdFileName,
                        ElementNames = s.Fields.Select(f => f.ElementName).ToList(),
                    })
                    .ToList();
                var documents = await _packageSessionRepository.GetDocumentsAsync(id);
                var dossiers = (await _packageSessionRepository.GetDossiersAsync(id))
                    .Where(d => d.Status != PackageSessionConst.FAILED)
                    .OrderBy(d => d.ExcelDocumentId)
                    .ThenBy(d => d.RowIndex)
                    .ToList();
                var sheetsByExcel = new Dictionary<int, Dictionary<int, (PackageSheetMatch Match, IReadOnlyList<SheetRow> Rows)>>();

                foreach (var dossier in dossiers)
                {
                    try
                    {
                        // Đọc lại Excel một lần cho mọi gói khai trong cùng file; ghép cột lại cho ra đúng MatchReport lúc kiểm
                        if (!sheetsByExcel.TryGetValue(dossier.ExcelDocumentId, out var sheets))
                        {
                            var excel = documents.First(d => d.Id == dossier.ExcelDocumentId);
                            var sheetNames = (await _excelReaderExternal.GetSheetHeadersAsync(excel.ObjectKey))
                                .ToDictionary(s => s.SheetName, s => s.SheetName, StringComparer.OrdinalIgnoreCase);
                            sheets = new Dictionary<int, (PackageSheetMatch Match, IReadOnlyList<SheetRow> Rows)>();
                            foreach (var schema in sessionSchemas)
                            {
                                var table = await _excelReaderExternal.ReadTableAsync(excel.ObjectKey, sheetNames[schema.Name]);
                                sheets[schema.MetadataType] = (PackageSessionDomainService.MatchSheetColumns(schema, table.Headers), table.Rows);
                            }
                            sheetsByExcel[dossier.ExcelDocumentId] = sheets;
                        }

                        var (packageMatch, packageRows) = sheets[PackageSchemaConst.METADATA_PACKAGE];
                        var packageRow = packageRows.FirstOrDefault(r => r.SheetRowNumber == dossier.RowIndex)
                            ?? throw new UserFriendlyException(ErrorCodes.BadRequest, $"File Excel không còn dòng {dossier.RowIndex} của gói");
                        var dossierDocuments = documents.Where(d => d.DossierId == dossier.Id).ToList();
                        var archive = new PackageArchiveDto
                        {
                            PackageType = session.PackageType,
                            Objid = PackageSessionDomainService.BuildPackageObjid(session.PackageType, session.FondsCode!, Guid.NewGuid()),
                            PackageCode = dossier.PackageCode!,
                            FondsCode = session.FondsCode!,
                            BuiltBy = session.BuiltBy!,
                            ArchiveCode = session.ArchiveCode,
                            ArchiveName = session.ArchiveName,
                            MetadataFilePrefix = packageSchema.MetadataFilePrefix,
                            Metadata = PackageSessionDomainService.ResolveArchiveFields(session.PackageType, packageSchema)
                                .Select(f => new PackageArchiveElementDto { ElementName = f.ElementName, Value = packageMatch.GetCell(packageRow, f.ElementName) })
                                .ToList(),
                            Schemas = archiveSchemas,
                            Files = dossierDocuments
                                .Where(d => d.FileRole == PackageSessionConst.FILE_ROLE_DATA)
                                .Select(d =>
                                {
                                    var schema = config.Schemas.First(s => s.MetadataType == d.MetadataType);
                                    var (match, rows) = sheets[schema.MetadataType];
                                    var row = PackageSessionDomainService.FindDocumentRow(match, rows, d.DocKey!);
                                    return (row.SheetRowNumber, File: new PackageArchiveFileDto
                                    {
                                        MetadataType = schema.MetadataType,
                                        DocKey = d.DocKey!,
                                        Extension = d.Extension,
                                        MimeType = d.MimeType,
                                        ObjectKey = d.ObjectKey,
                                        Sha256 = d.Sha256,
                                        MetadataFilePrefix = schema.MetadataFilePrefix,
                                        Metadata = PackageSessionDomainService.ResolveArchiveFields(session.PackageType, schema)
                                            .Select(f => new PackageArchiveElementDto { ElementName = f.ElementName, Value = match.GetCell(row, f.ElementName) })
                                            .ToList(),
                                    });
                                })
                                .OrderBy(f => f.File.MetadataType)
                                .ThenBy(f => f.SheetRowNumber)
                                .Select(f => f.File)
                                .ToList(),
                            DocumentationFile = dossierDocuments
                                .Where(d => d.FileRole == PackageSessionConst.FILE_ROLE_DIP_AUTH)
                                .Select(d => new PackageArchiveFileDto
                                {
                                    DocKey = dossier.PackageCode!,
                                    Extension = d.Extension,
                                    MimeType = d.MimeType,
                                    ObjectKey = d.ObjectKey,
                                    Sha256 = d.Sha256,
                                })
                                .FirstOrDefault(),
                        };

                        var packageFileName = $"{Guid.NewGuid():D}{PackageBuildConst.ZIP_EXTENSION}";
                        var packageObjectKey = $"{PackageSessionDomainService.BuildPackagesObjectKeyPrefix(id)}{packageFileName}";
                        var packageSize = await _packageArchiveExternal.BuildAsync(archive, packageObjectKey);
                        dossier.CompleteBuild(archive.Objid, packageObjectKey, packageFileName, packageSize, userId, GetVietnamTime());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{nameof(BuildFromJob)}, id={id}, dossierId={dossier.Id}");
                        dossier.FailBuild(ex is UserFriendlyException ? ex.Message : PackageBuildConst.BUILD_FAILED_MESSAGE, userId, GetVietnamTime());
                    }
                    session.AddBuildProgress(userId, GetVietnamTime());
                    await _unitOfWork.SaveChangesAsync();
                }

                session.FinishBuild(dossiers, userId, GetVietnamTime());
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(BuildFromJob)}, id={id}");
                session.FailBuild(ex is UserFriendlyException ? ex.Message : PackageBuildConst.BUILD_FAILED_MESSAGE, userId, GetVietnamTime());
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
