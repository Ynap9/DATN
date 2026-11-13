using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using sh.be.applications.Module.Operation.Dtos.Wage;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;

namespace sh.be.infrastructure.data.Module.Operation.Queries
{
    public class WageQuery : BaseQuery, IWageQuery
    {
        private readonly IMemoryCache _memoryCache;

        public WageQuery(ShDbContext shDbContext, IMapper mapper, IMemoryCache memoryCache) : base(shDbContext, mapper)
        {
            _memoryCache = memoryCache;
        }

        // Mọi thành viên Data / QA của dự án, kể cả đã gỡ (để tính tiền văn bản đã kiểm đang giữ)
        public Task<List<WageMemberDto>> FindMembers(int projectId)
        {
            // IgnoreQueryFilters: thành viên đã gỡ vẫn còn văn bản đã kiểm đang giữ thì vẫn được tính tiền
            return _shDbContext.ProjectMembers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.ProjectId == projectId)
                .Select(m => new WageMemberDto
                {
                    MemberId = m.Id,
                    UserId = m.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.UserName).FirstOrDefault() ?? "",
                    FullName = _shDbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault(),
                    DocumentTypeId = m.DocumentTypeId,
                    DocumentTypeName = _shDbContext.DocumentTypes.Where(d => d.Id == m.DocumentTypeId).Select(d => d.Name).FirstOrDefault() ?? "",
                    Role = m.Role,
                    ManagedByUserId = m.ManagedByUserId,
                    IsActive = m.IsActive,
                    IsRemoved = m.Deleted,
                    CompanyPrice = m.CompanyPrice,
                    ActualPrice = m.ActualPrice,
                })
                .ToListAsync();
        }

        // Khối lượng Data / QA theo (loại văn bản, người), cache 60 giây; tiền không cache
        public async Task<Dictionary<(int DocumentTypeId, string UserId), WageQuantityDto>> FindQuantities(int projectId)
        {
            return (await _memoryCache.GetOrCreateAsync($"wage:{projectId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(PriceConst.WAGE_CACHE_SECONDS);

                var heldDocuments = _shDbContext.SubmissionUsers
                    .AsNoTracking()
                    .Where(su => su.ProjectId == projectId && su.Role == RoleConstants.ROLE_DATA
                        && _shDbContext.Submissions.Any(s => s.Id == su.SubmissionId)
                        && _shDbContext.SubmissionLogs.Any(l => l.SubmissionId == su.SubmissionId && l.CreatedDate >= su.CreatedDate))
                    .Select(su => new { su.DocumentTypeId, su.UserId, su.SubmissionId })
                    .Distinct();
                var dataCounts = await heldDocuments
                    .Select(x => new
                    {
                        x.DocumentTypeId,
                        x.UserId,
                        FieldCount = _shDbContext.SubmissionDatas.Count(d => d.SubmissionId == x.SubmissionId && d.Data.Trim() != ""),
                    })
                    .GroupBy(x => new { x.DocumentTypeId, x.UserId })
                    .Select(g => new { g.Key.DocumentTypeId, g.Key.UserId, DocumentCount = g.Count(), FieldCount = g.Sum(x => x.FieldCount) })
                    .ToListAsync();
                var reviewCounts = await _shDbContext.SubmissionQaLogs
                    .AsNoTracking()
                    .Where(l => l.ProjectId == projectId && _shDbContext.Submissions.Any(s => s.Id == l.SubmissionId))
                    .GroupBy(l => new { l.DocumentTypeId, l.UserId })
                    .Select(g => new
                    {
                        g.Key.DocumentTypeId,
                        g.Key.UserId,
                        ReviewCount = g.Count(),
                        PassedCount = g.Count(l => l.NewStatusQa == SubmissionConst.QA_PASSED),
                        FailedCount = g.Count(l => l.NewStatusQa == SubmissionConst.QA_FAILED),
                    })
                    .ToListAsync();

                var quantities = new Dictionary<(int DocumentTypeId, string UserId), WageQuantityDto>();
                foreach (var item in dataCounts)
                {
                    quantities[(item.DocumentTypeId, item.UserId)] = new WageQuantityDto { DocumentCount = item.DocumentCount, FieldCount = item.FieldCount };
                }
                foreach (var item in reviewCounts)
                {
                    if (!quantities.TryGetValue((item.DocumentTypeId, item.UserId), out var quantity))
                    {
                        quantity = new WageQuantityDto();
                        quantities[(item.DocumentTypeId, item.UserId)] = quantity;
                    }
                    quantity.ReviewCount = item.ReviewCount;
                    quantity.PassedCount = item.PassedCount;
                    quantity.FailedCount = item.FailedCount;
                }
                return quantities;
            }))!;
        }
    }
}
