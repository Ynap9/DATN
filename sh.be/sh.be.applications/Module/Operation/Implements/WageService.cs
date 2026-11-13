using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Operation.Dtos.Wage;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.domain.Module.Operation.Wages.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Operation.Implements
{
    public class WageService : BaseService, IWageService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWageQuery _wageQuery;

        public WageService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<WageService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectMemberRepository projectMemberRepository, IRoleRepository roleRepository, IWageQuery wageQuery
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectMemberRepository = projectMemberRepository;
            _roleRepository = roleRepository;
            _wageQuery = wageQuery;
        }

        // Thống kê tiền công theo phạm vi người gọi: Admin thấy tất cả, Leader thấy người mình quản lý, còn lại chỉ thấy mình
        public async Task<ViewWagePagingDto> FindPaging(int projectId, FindPagingWageDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > ProjectMemberConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {ProjectMemberConst.MAX_PAGE_SIZE}");
            }
            if (dto.Role != null && dto.Role is not (RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
            var project = await _projectMemberRepository.FindProjectAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            if (dto.DocumentTypeId.HasValue && !await _projectMemberRepository.IsDocumentTypeInProjectAsync(projectId, dto.DocumentTypeId.Value))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotInProject);
            }

            var currentUserId = getCurrentUserId();
            var isAdmin = PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var members = await _wageQuery.FindMembers(projectId);
            var quantities = await _wageQuery.FindQuantities(projectId);
            var scopeMembers = members.Select(m => new WageMember(m.DocumentTypeId, m.UserId, m.Role, m.ManagedByUserId, m.IsActive, m.IsRemoved)).ToList();
            var leadingDocumentTypeIds = WageDomainService.FindLeadingDocumentTypeIds(scopeMembers, currentUserId);

            List<GetListWageDto> rows = [];
            foreach (var (member, scopeMember) in members.Zip(scopeMembers))
            {
                if (member.Role == RoleConstants.ROLE_LEADER || !WageDomainService.CanViewMember(scopeMember, currentUserId, isAdmin, leadingDocumentTypeIds))
                {
                    continue;
                }
                var quantity = member.Role == RoleConstants.ROLE_QA && !project.IsQaEnabled
                    ? new WageQuantityDto()
                    : quantities.GetValueOrDefault((member.DocumentTypeId, member.UserId)) ?? new WageQuantityDto();
                var quantityValue = member.Role == RoleConstants.ROLE_DATA ? quantity.FieldCount : quantity.ReviewCount;
                if (member.IsRemoved && quantityValue == 0)
                {
                    continue;
                }
                var canViewCompanyPrice = WageDomainService.CanViewCompanyPrice(scopeMember, currentUserId, isAdmin, leadingDocumentTypeIds);
                rows.Add(new GetListWageDto
                {
                    MemberId = member.MemberId,
                    UserId = member.UserId,
                    UserName = member.UserName,
                    FullName = member.FullName,
                    DocumentTypeId = member.DocumentTypeId,
                    DocumentTypeName = member.DocumentTypeName,
                    Role = member.Role,
                    IsActive = member.IsActive,
                    IsRemoved = member.IsRemoved,
                    Quantity = quantityValue,
                    DocumentCount = quantity.DocumentCount,
                    ReviewCount = quantity.ReviewCount,
                    PassedCount = quantity.PassedCount,
                    FailedCount = quantity.FailedCount,
                    CompanyPrice = canViewCompanyPrice ? member.CompanyPrice : null,
                    ActualPrice = member.ActualPrice,
                    IsPriceMissing = member.ActualPrice == null,
                    ActualAmount = WageDomainService.CalculateAmount(quantityValue, member.ActualPrice),
                    CompanyAmount = canViewCompanyPrice ? WageDomainService.CalculateAmount(quantityValue, member.CompanyPrice) : null,
                });
            }

            var filtered = rows
                .Where(r => dto.DocumentTypeId == null || r.DocumentTypeId == dto.DocumentTypeId)
                .Where(r => dto.Role == null || r.Role == dto.Role)
                .Where(r => string.IsNullOrEmpty(dto.Keyword) || r.UserName.Contains(dto.Keyword, StringComparison.OrdinalIgnoreCase)
                    || (r.FullName ?? "").Contains(dto.Keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.DocumentTypeId)
                .ThenBy(r => r.Role)
                .ThenBy(r => r.FullName)
                .ToList();
            var companyRows = filtered.Where(r => r.CompanyAmount.HasValue).ToList();

            return new ViewWagePagingDto
            {
                Items = filtered.Skip(dto.GetSkip()).Take(dto.PageSize).ToList(),
                TotalItems = filtered.Count,
                Summary = new ViewWageSummaryDto
                {
                    DataQuantity = filtered.Where(r => r.Role == RoleConstants.ROLE_DATA).Sum(r => r.Quantity),
                    QaQuantity = filtered.Where(r => r.Role == RoleConstants.ROLE_QA).Sum(r => r.Quantity),
                    ActualAmount = filtered.Sum(r => r.ActualAmount),
                    CompanyAmount = companyRows.Count > 0 ? companyRows.Sum(r => r.CompanyAmount!.Value) : null,
                },
            };
        }
    }
}
