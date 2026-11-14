using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Base.Interfaces.External.DataReader;
using sh.be.applications.Module.Operation.Dtos.Wage;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.domain.Module.Operation.Wages.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Globalization;
using System.Text.Json;

namespace sh.be.applications.Module.Operation.Implements
{
    public class WageService : BaseService, IWageService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IWageQuery _wageQuery;
        private readonly IExcelExternal _excelExternal;

        public WageService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<WageService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectMemberRepository projectMemberRepository, IRoleRepository roleRepository, IWageQuery wageQuery, IExcelExternal excelExternal
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectMemberRepository = projectMemberRepository;
            _roleRepository = roleRepository;
            _wageQuery = wageQuery;
            _excelExternal = excelExternal;
        }

        // Thống kê tiền công theo phạm vi người gọi: Admin thấy tất cả, Leader thấy người mình quản lý, còn lại chỉ thấy mình
        public async Task<ViewWagePagingDto> FindPaging(int projectId, FindPagingWageDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > ProjectMemberConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {ProjectMemberConst.MAX_PAGE_SIZE}");
            }

            var currentUserId = getCurrentUserId();
            var isAdmin = PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            var (_, rows) = await BuildWageRowsAsync(projectId, dto.DocumentTypeId, dto.Role, dto.Keyword, currentUserId, isAdmin);

            return new ViewWagePagingDto
            {
                Items = rows.Skip(dto.GetSkip()).Take(dto.PageSize).ToList(),
                TotalItems = rows.Count,
                Summary = SummarizeRows(rows),
            };
        }

        public async Task<ExportFileDto> ExportExcel(int projectId, ExportWageDto dto)
        {
            _logger.LogInformation($"{nameof(ExportExcel)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var isAdmin = PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            WageDomainService.EnsureCanExport(isAdmin);
            var (project, rows) = await BuildWageRowsAsync(projectId, dto.DocumentTypeId, dto.Role, dto.Keyword, currentUserId, isAdmin);
            var summary = SummarizeRows(rows);

            var headers = new List<string>
            {
                "STT", "Loại văn bản", "Tài khoản", "Họ tên", "Vai trò", "Trạng thái", "Số văn bản", "Khối lượng",
                "Lượt đạt", "Lượt không đạt", "Đơn giá thực nhận", "Thành tiền thực nhận", "Đơn giá công ty", "Thành tiền công ty",
            };
            var table = rows.Select((r, index) => (IReadOnlyList<string>)new List<string>
            {
                (index + 1).ToString(),
                r.DocumentTypeName,
                r.UserName,
                string.IsNullOrWhiteSpace(r.FullName) ? r.UserName : r.FullName,
                r.Role,
                r.IsRemoved ? "Đã gỡ" : !r.IsActive ? "Đang tắt" : "Đang làm",
                (r.Role == RoleConstants.ROLE_QA ? r.ReviewDocumentCount : r.DocumentCount).ToString(),
                r.Quantity.ToString(),
                r.Role == RoleConstants.ROLE_QA ? r.PassedCount.ToString() : "",
                r.Role == RoleConstants.ROLE_QA ? r.FailedCount.ToString() : "",
                FormatMoney(r.ActualPrice),
                FormatMoney(r.ActualAmount),
                FormatMoney(r.CompanyPrice),
                FormatMoney(r.CompanyAmount),
            }).ToList();
            table.Add(new List<string>
            {
                "Tổng", "", "", "", "", "", "",
                $"Data: {summary.DataQuantity}, QA: {summary.QaQuantity}",
                "", "", "",
                FormatMoney(summary.ActualAmount),
                "",
                FormatMoney(summary.CompanyAmount ?? 0m),
            });

            return _excelExternal.CreateTable($"TienCong_{project.Name}_{GetVietnamTime():yyyyMMddHHmm}", "Tiền công", headers, table);
        }

        private async Task<(Project Project, List<GetListWageDto> Rows)> BuildWageRowsAsync(int projectId, int? documentTypeId, string? role, string? keyword, string currentUserId, bool isAdmin)
        {
            if (role != null && role is not (RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
            var project = await _projectMemberRepository.FindProjectAsync(projectId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotFound);
            if (documentTypeId.HasValue && !await _projectMemberRepository.IsDocumentTypeInProjectAsync(projectId, documentTypeId.Value))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorDocumentTypeNotInProject);
            }

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
                    ReviewDocumentCount = quantity.ReviewDocumentCount,
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
                .Where(r => documentTypeId == null || r.DocumentTypeId == documentTypeId)
                .Where(r => role == null || r.Role == role)
                .Where(r => string.IsNullOrEmpty(keyword) || r.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || (r.FullName ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.DocumentTypeId)
                .ThenBy(r => r.Role)
                .ThenBy(r => r.FullName)
                .ToList();
            return (project, filtered);
        }

        private static ViewWageSummaryDto SummarizeRows(List<GetListWageDto> rows)
        {
            var companyRows = rows.Where(r => r.CompanyAmount.HasValue).ToList();
            return new ViewWageSummaryDto
            {
                DataQuantity = rows.Where(r => r.Role == RoleConstants.ROLE_DATA).Sum(r => r.Quantity),
                QaQuantity = rows.Where(r => r.Role == RoleConstants.ROLE_QA).Sum(r => r.Quantity),
                ActualAmount = rows.Sum(r => r.ActualAmount),
                CompanyAmount = companyRows.Count > 0 ? companyRows.Sum(r => r.CompanyAmount!.Value) : null,
            };
        }

        private static string FormatMoney(decimal? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? "";
        }
    }
}
