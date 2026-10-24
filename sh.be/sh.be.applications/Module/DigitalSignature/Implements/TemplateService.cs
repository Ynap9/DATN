using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.Storage;
using sh.be.applications.Module.DigitalSignature.Dtos.Template;
using sh.be.applications.Module.DigitalSignature.Interfaces.Queries;
using sh.be.applications.Module.DigitalSignature.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.DigitalSignature.Entities;
using sh.be.domain.Module.DigitalSignature.Repositories;
using sh.be.domain.Module.DigitalSignature.Services;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.DigitalSignature;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.DigitalSignature.Implements
{
    public class TemplateService : BaseService, ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ITemplateQuery _templateQuery;
        private readonly IFileStorageExternal _fileStorageExternal;
        private readonly ISampleFileExternal _sampleFileExternal;

        public TemplateService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<TemplateService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            ITemplateRepository templateRepository, ITemplateQuery templateQuery, IFileStorageExternal fileStorageExternal, ISampleFileExternal sampleFileExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _templateRepository = templateRepository;
            _templateQuery = templateQuery;
            _fileStorageExternal = fileStorageExternal;
            _sampleFileExternal = sampleFileExternal;
        }

        public async Task<BaseResponsePagingDto<GetListTemplateDto>> FindPaging(FindPagingTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            return await _templateQuery.FindPaging(dto);
        }

        public async Task<FindTemplateByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            return await _templateQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);
        }

        public ExportFileDto GetSampleFile()
        {
            _logger.LogInformation($"{nameof(GetSampleFile)}");
            return _sampleFileExternal.ReadSigningSampleFile();
        }

        public async Task<FindTemplateByIdDto> Create(CreateTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            var template = Template.Create(dto.Name, dto.Description, getCurrentUserId(), GetVietnamTime());

            _templateRepository.Add(template);
            AddAuditLog(AuditLogConstants.ACTION_TEMPLATE_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo mẫu chữ ký {template.Name}",
                null, new { template.Name, template.Description });
            await _unitOfWork.SaveChangesAsync();

            return await FindById(template.Id);
        }

        public async Task Update(int id, UpdateTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var template = await _templateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);
            var oldData = new { template.Id, template.Name };

            template.Update(dto.Name, getCurrentUserId(), GetVietnamTime());

            _templateRepository.Update(template);
            AddAuditLog(AuditLogConstants.ACTION_TEMPLATE_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật mẫu chữ ký {template.Name}",
                oldData, new { template.Id, template.Name });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<FindTemplateByIdDto> UpdateConfig(int id, UpdateConfigTemplateDto dto)
        {
            _logger.LogInformation($"{nameof(UpdateConfig)}, id={id}, positionCount={dto.Positions.Count}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var template = await _templateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);
            var oldData = ToConfigAuditData(template);
            var oldImageObjectKeys = new[] { template.StampImageObjectKey, template.WetSignatureImageObjectKey };

            template.UpdateConfig(dto.SigningReason, dto.SigningLocation, dto.IsShowDigitalSignature, dto.IsDigitalSignatureInImage,
                dto.AllowSigningSignedPdf, dto.StampIntensity, dto.WetSignatureIntensity, dto.WetSignatureStrokeWidth,
                dto.DigitalSignatureColor, dto.WetSignatureColor, dto.DigitalSignatureFont, dto.DigitalSignatureFontSize,
                currentUserId, now);
            var positions = dto.Positions
                .Select(p => template.CreatePosition(p.Kind, p.PageNumber, p.XRatio, p.YRatio, p.WidthRatio, p.HeightRatio, currentUserId, now))
                .ToList();
            await ChangeImagesAsync(template, dto);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _templateRepository.ReplacePositionsAsync(id, positions);
                _templateRepository.Update(template);
                AddAuditLog(AuditLogConstants.ACTION_TEMPLATE_UPDATE_CONFIG, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật cấu hình mẫu chữ ký {template.Name}",
                    oldData, ToConfigAuditData(template));
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(UpdateConfig)}, id={id}");
                await transaction.RollbackAsync();
                throw;
            }

            await DeleteObsoleteImagesAsync(oldImageObjectKeys, template);
            return await FindById(id);
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var template = await _templateRepository.FindByIdAsync(id)
                ?? throw new UserFriendlyException(ErrorCodes.DigitalSignatureErrorTemplateNotFound);
            var positions = await _templateRepository.GetPositionsAsync(id);

            template.Delete(currentUserId, now);
            positions.ForEach(p => p.Delete(currentUserId, now));

            _templateRepository.Update(template);
            AddAuditLog(AuditLogConstants.ACTION_TEMPLATE_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa mẫu chữ ký {template.Name}",
                new { template.Id, template.Name }, null);
            await _unitOfWork.SaveChangesAsync();

            await _fileStorageExternal.DeleteByPrefixAsync(TemplateImagePolicy.BuildObjectKeyPrefix(id));
        }

        private async Task ChangeImagesAsync(Template template, UpdateConfigTemplateDto dto)
        {
            EnsureValidImage(dto.StampImage);
            EnsureValidImage(dto.WetSignatureImage);

            if (dto.StampImage != null)
            {
                var objectKey = TemplateImagePolicy.BuildObjectKey(template.Id, TemplateConst.STAMP_IMAGE_NAME, dto.StampImage.FileName);
                template.ChangeStampImage(await UploadImageAsync(dto.StampImage, objectKey), objectKey);
            }
            else if (dto.IsRemoveStampImage)
            {
                template.RemoveStampImage();
            }

            if (dto.WetSignatureImage != null)
            {
                var objectKey = TemplateImagePolicy.BuildObjectKey(template.Id, TemplateConst.WET_SIGNATURE_IMAGE_NAME, dto.WetSignatureImage.FileName);
                template.ChangeWetSignatureImage(await UploadImageAsync(dto.WetSignatureImage, objectKey), objectKey);
            }
            else if (dto.IsRemoveWetSignatureImage)
            {
                template.RemoveWetSignatureImage();
            }
        }

        private static void EnsureValidImage(IFormFile? image)
        {
            if (image != null)
            {
                TemplateImagePolicy.EnsureValidImage(image.FileName, image.Length);
            }
        }

        private async Task<string> UploadImageAsync(IFormFile image, string objectKey)
        {
            await using var content = image.OpenReadStream();
            return await _fileStorageExternal.UploadAsync(content, objectKey, image.ContentType);
        }

        private async Task DeleteObsoleteImagesAsync(IEnumerable<string?> oldImageObjectKeys, Template template)
        {
            var currentImageObjectKeys = new[] { template.StampImageObjectKey, template.WetSignatureImageObjectKey }.OfType<string>();
            foreach (var objectKey in oldImageObjectKeys.OfType<string>().Except(currentImageObjectKeys))
            {
                await _fileStorageExternal.DeleteAsync(objectKey);
            }
        }

        private static object ToConfigAuditData(Template template)
        {
            return new
            {
                template.Id,
                template.SigningReason,
                template.SigningLocation,
                template.StampImageObjectKey,
                template.WetSignatureImageObjectKey,
                template.IsShowDigitalSignature,
                template.IsDigitalSignatureInImage,
                template.AllowSigningSignedPdf,
                template.StampIntensity,
                template.WetSignatureIntensity,
                template.WetSignatureStrokeWidth,
                template.DigitalSignatureColor,
                template.WetSignatureColor,
                template.DigitalSignatureFont,
                template.DigitalSignatureFontSize,
            };
        }
    }
}
