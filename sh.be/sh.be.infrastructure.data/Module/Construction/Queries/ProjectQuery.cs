using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using System.Text.Json;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class ProjectQuery : BaseQuery, IProjectQuery
    {
        public ProjectQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        // Danh sách dự án: Admin mọi dự án, còn lại dự án mình là thành viên chưa gỡ; link chỉ Admin / Leader của chính dự án đó thấy
        public Task<List<GetListProjectDto>> GetList(string userId, bool isAdmin)
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .Where(p => isAdmin || _shDbContext.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId))
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Select(p => new GetListProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StartedAt = p.StartedAt,
                    EndedAt = p.EndedAt,
                    Status = p.Status,
                    UrlGoogleSheetSync = isAdmin || _shDbContext.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId && m.Role == RoleConstants.ROLE_LEADER)
                        ? p.UrlGoogleSheetSync
                        : null,
                    IsQaEnabled = p.IsQaEnabled,
                })
                .ToListAsync();
        }

        // Dropdown dự án: Admin mọi dự án, còn lại dự án mình là thành viên chưa gỡ
        public Task<List<GetListProjectDropDownDto>> GetProjectDropDown(string userId, bool isAdmin)
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .Where(p => isAdmin || _shDbContext.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId))
                .OrderBy(p => p.Name)
                .ProjectTo<GetListProjectDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        // Paging cặp (dự án, loại văn bản): Admin mọi cặp, còn lại cặp mình là thành viên; Admin / Leader xem số tổng, Data / QA xem số văn bản mình giữ.
        // Lấy trang cặp trước rồi đếm cho đúng các cặp của trang bằng GROUP BY, số câu SQL cố định
        public async Task<BaseResponsePagingDto<GetListProjectPairDto>> FindPaging(FindPagingProjectDto dto, string userId, bool isAdmin)
        {
            var query = from pd in _shDbContext.ProjectDocumentTypes.AsNoTracking()
                        join p in _shDbContext.Projects on pd.ProjectId equals p.Id
                        join d in _shDbContext.DocumentTypes on pd.DocumentTypeId equals d.Id into documentTypes
                        from d in documentTypes.DefaultIfEmpty()
                        where isAdmin || _shDbContext.ProjectMembers.Any(m => m.ProjectId == pd.ProjectId && m.DocumentTypeId == pd.DocumentTypeId && m.UserId == userId)
                        select new { Project = p, pd.DocumentTypeId, DocumentTypeName = d != null ? d.Name : null };
            if (dto.Status.HasValue)
            {
                query = query.Where(x => x.Project.Status == dto.Status);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(x => x.Project.Name.Contains(dto.Keyword));
            }
            if (dto.DocumentTypeId.HasValue)
            {
                query = query.Where(x => x.DocumentTypeId == dto.DocumentTypeId);
            }

            var totalCount = await query.CountAsync();

            // Loại văn bản đã xoá (tên null qua query filter) xếp cuối trong dự án, như tổng hợp nghiệm thu
            var pairs = await query
                .OrderByDescending(x => x.Project.CreatedDate)
                .ThenByDescending(x => x.Project.Id)
                .ThenBy(x => x.DocumentTypeName == null)
                .ThenBy(x => x.DocumentTypeName)
                .Paging(dto)
                .Select(x => new GetListProjectPairDto
                {
                    ProjectId = x.Project.Id,
                    ProjectName = x.Project.Name,
                    Status = x.Project.Status,
                    StartedAt = x.Project.StartedAt,
                    EndedAt = x.Project.EndedAt,
                    IsQaEnabled = x.Project.IsQaEnabled,
                    UrlGoogleSheetSync = x.Project.UrlGoogleSheetSync,
                    DocumentTypeId = x.DocumentTypeId,
                    ViewerRole = isAdmin
                        ? RoleConstants.ROLE_ADMIN
                        : _shDbContext.ProjectMembers
                            .Where(m => m.ProjectId == x.Project.Id && m.DocumentTypeId == x.DocumentTypeId && m.UserId == userId)
                            .Select(m => m.Role)
                            .FirstOrDefault() ?? String.Empty,
                })
                .ToListAsync();

            var projectIds = pairs.Select(x => x.ProjectId).Distinct().ToList();
            var documentTypeIds = pairs.Select(x => x.DocumentTypeId).Distinct().ToList();
            var documentTypeNames = await _shDbContext.DocumentTypes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(d => documentTypeIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);
            var memberCounts = await _shDbContext.ProjectMembers
                .AsNoTracking()
                .Where(m => projectIds.Contains(m.ProjectId) && documentTypeIds.Contains(m.DocumentTypeId))
                .GroupBy(m => new { m.ProjectId, m.DocumentTypeId })
                .Select(g => new { g.Key.ProjectId, g.Key.DocumentTypeId, Count = g.Count() })
                .ToDictionaryAsync(c => (c.ProjectId, c.DocumentTypeId), c => c.Count);
            var totalCounts = await _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => projectIds.Contains(s.ProjectId) && documentTypeIds.Contains(s.DocumentTypeId))
                .GroupBy(s => new { s.ProjectId, s.DocumentTypeId })
                .Select(g => new
                {
                    g.Key.ProjectId,
                    g.Key.DocumentTypeId,
                    Counts = new PairCounts(
                        g.Count(),
                        g.Select(s => s.Box).Distinct().Count(),
                        g.Select(s => s.Box + "|" + s.ProfileRange).Distinct().Count(),
                        g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                        g.Count(s => s.StatusCheck == SubmissionConst.CHECKED && s.StatusQa == SubmissionConst.QA_PASSED)),
                })
                .ToDictionaryAsync(c => (c.ProjectId, c.DocumentTypeId), c => c.Counts);

            var (myCounts, myFilledFieldCounts) = await CountMyHeldSubmissionsAsync(projectIds, documentTypeIds, userId);

            foreach (var pair in pairs)
            {
                var key = (pair.ProjectId, pair.DocumentTypeId);
                var isTotalViewer = pair.ViewerRole is RoleConstants.ROLE_ADMIN or RoleConstants.ROLE_LEADER;
                var counts = (isTotalViewer ? totalCounts.GetValueOrDefault(key) : myCounts.GetValueOrDefault((pair.ProjectId, pair.DocumentTypeId, pair.ViewerRole)))
                    ?? new PairCounts(0, 0, 0, 0, 0);

                pair.DocumentTypeName = documentTypeNames.GetValueOrDefault(pair.DocumentTypeId) ?? String.Empty;
                pair.UrlGoogleSheetSync = isTotalViewer ? pair.UrlGoogleSheetSync : null;
                pair.MemberCount = memberCounts.GetValueOrDefault(key);
                pair.TotalBox = counts.TotalBox;
                pair.TotalProfileRange = counts.TotalProfileRange;
                pair.TotalSubmission = counts.Total;
                pair.CheckedCount = counts.Checked;
                pair.QaPassedCount = isTotalViewer && pair.IsQaEnabled ? counts.QaPassed : null;
                pair.ProgressPercent = counts.Total == 0 ? 0 : Math.Round(counts.Checked * 100m / counts.Total, 2);
                pair.FilledFieldCount = pair.ViewerRole == RoleConstants.ROLE_DATA ? myFilledFieldCounts.GetValueOrDefault(key) : null;
            }

            return new BaseResponsePagingDto<GetListProjectPairDto>
            {
                Items = pairs,
                TotalItems = totalCount,
            };
        }

        public Task<FindProjectByIdDto?> FindById(int id)
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .Where(p => p.Id == id)
                .ProjectTo<FindProjectByIdDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        // Tiến trình của tôi theo từng cặp mình là Data / QA chưa gỡ của dự án, cùng số với cột "của tôi" ở paging
        public async Task<List<GetListProjectMyProgressDto>> FindMyProgresses(int projectId, string userId)
        {
            // Loại văn bản đã xoá (tên null qua query filter) xếp cuối, như paging
            var pairs = await (from m in _shDbContext.ProjectMembers.AsNoTracking()
                               join pd in _shDbContext.ProjectDocumentTypes on new { m.ProjectId, m.DocumentTypeId } equals new { pd.ProjectId, pd.DocumentTypeId }
                               join d in _shDbContext.DocumentTypes on m.DocumentTypeId equals d.Id into documentTypes
                               from d in documentTypes.DefaultIfEmpty()
                               where m.ProjectId == projectId && m.UserId == userId && (m.Role == RoleConstants.ROLE_DATA || m.Role == RoleConstants.ROLE_QA)
                               select new { m.DocumentTypeId, m.Role, DocumentTypeName = d != null ? d.Name : null })
                               .OrderBy(x => x.DocumentTypeName == null)
                               .ThenBy(x => x.DocumentTypeName)
                               .ToListAsync();

            var documentTypeIds = pairs.Select(x => x.DocumentTypeId).ToList();
            var documentTypeNames = await _shDbContext.DocumentTypes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(d => documentTypeIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);
            var (myCounts, myFilledFieldCounts) = await CountMyHeldSubmissionsAsync(new List<int> { projectId }, documentTypeIds, userId);

            return pairs.Select(pair =>
            {
                var counts = myCounts.GetValueOrDefault((projectId, pair.DocumentTypeId, pair.Role)) ?? new PairCounts(0, 0, 0, 0, 0);
                return new GetListProjectMyProgressDto
                {
                    DocumentTypeId = pair.DocumentTypeId,
                    DocumentTypeName = documentTypeNames.GetValueOrDefault(pair.DocumentTypeId) ?? String.Empty,
                    ViewerRole = pair.Role,
                    TotalBox = counts.TotalBox,
                    TotalProfileRange = counts.TotalProfileRange,
                    TotalSubmission = counts.Total,
                    CheckedCount = counts.Checked,
                    ProgressPercent = counts.Total == 0 ? 0 : Math.Round(counts.Checked * 100m / counts.Total, 2),
                    FilledFieldCount = pair.Role == RoleConstants.ROLE_DATA ? myFilledFieldCounts.GetValueOrDefault((projectId, pair.DocumentTypeId)) : null,
                };
            }).ToList();
        }

        public async Task<ViewProjectAcceptanceDto> FindAcceptanceSummary(int projectId)
        {
            var countsByDocumentTypeId = await _shDbContext.Submissions
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId)
                .GroupBy(s => s.DocumentTypeId)
                .Select(g => new GetListProjectAcceptanceSummaryDto
                {
                    DocumentTypeId = g.Key,
                    TotalSubmissions = g.Count(),
                    TotalNotChecked = g.Count(s => s.StatusCheck == SubmissionConst.NOT_CHECKED),
                    TotalChecked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                    TotalReturned = g.Count(s => s.StatusCheck == SubmissionConst.RETURNED),
                    TotalQaNotReviewed = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED && s.StatusQa == SubmissionConst.QA_NOT_REVIEWED),
                    TotalQaPassed = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED && s.StatusQa == SubmissionConst.QA_PASSED),
                    TotalQaFailed = g.Count(s => s.StatusQa == SubmissionConst.QA_FAILED),
                })
                .ToDictionaryAsync(c => c.DocumentTypeId!.Value);

            // Loại văn bản đã gỡ khỏi dự án mà còn văn bản vẫn hiện (IsRemoved) để dòng tổng bằng tổng các dòng
            var attachedDocumentTypeIds = await _shDbContext.ProjectDocumentTypes
                .AsNoTracking()
                .Where(pd => pd.ProjectId == projectId)
                .Select(pd => pd.DocumentTypeId)
                .ToListAsync();
            var documentTypeIds = attachedDocumentTypeIds.Union(countsByDocumentTypeId.Keys).ToList();
            var documentTypeNames = await _shDbContext.DocumentTypes
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(d => documentTypeIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            var documentTypes = documentTypeIds
                .Select(documentTypeId =>
                {
                    var row = countsByDocumentTypeId.GetValueOrDefault(documentTypeId) ?? new GetListProjectAcceptanceSummaryDto { DocumentTypeId = documentTypeId };
                    row.DocumentTypeName = documentTypeNames.GetValueOrDefault(documentTypeId);
                    row.IsRemoved = !attachedDocumentTypeIds.Contains(documentTypeId);
                    row.QaPassedPercent = CalculateQaPassedPercent(row.TotalQaPassed, row.TotalSubmissions);
                    return row;
                })
                .OrderBy(row => row.IsRemoved)
                .ThenBy(row => row.DocumentTypeName)
                .ToList();

            var total = new GetListProjectAcceptanceSummaryDto
            {
                TotalSubmissions = documentTypes.Sum(row => row.TotalSubmissions),
                TotalNotChecked = documentTypes.Sum(row => row.TotalNotChecked),
                TotalChecked = documentTypes.Sum(row => row.TotalChecked),
                TotalReturned = documentTypes.Sum(row => row.TotalReturned),
                TotalQaNotReviewed = documentTypes.Sum(row => row.TotalQaNotReviewed),
                TotalQaPassed = documentTypes.Sum(row => row.TotalQaPassed),
                TotalQaFailed = documentTypes.Sum(row => row.TotalQaFailed),
            };
            total.QaPassedPercent = CalculateQaPassedPercent(total.TotalQaPassed, total.TotalSubmissions);

            return new ViewProjectAcceptanceDto
            {
                ProjectId = projectId,
                DocumentTypes = documentTypes,
                Total = total,
            };
        }

        public async Task<ViewProjectAcceptanceTransitionDto?> FindLastAcceptanceTransition(int projectId)
        {
            // Audit nghiệm thu luôn ghi ProjectId là trường đầu của NewData, nên lọc theo tiền tố JSON thay cho cột ProjectId mà AuditLog không có
            var newDataPrefix = $"{{\"ProjectId\":{projectId},";
            var auditLog = await _shDbContext.AuditLogs
                .AsNoTracking()
                .Where(a => AcceptanceActions.Contains(a.Action) && a.NewData!.StartsWith(newDataPrefix))
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.Id)
                .Select(a => new
                {
                    a.Action,
                    a.CreatedDate,
                    a.UserId,
                    UserName = _shDbContext.Users.Where(u => u.Id == a.UserId).Select(u => u.UserName).FirstOrDefault(),
                    FullName = _shDbContext.Users.Where(u => u.Id == a.UserId).Select(u => u.FullName).FirstOrDefault(),
                    a.OldData,
                    a.NewData,
                })
                .FirstOrDefaultAsync();
            if (auditLog == null)
            {
                return null;
            }

            var oldData = JsonSerializer.Deserialize<AcceptanceAuditData>(auditLog.OldData ?? "{}");
            var newData = JsonSerializer.Deserialize<AcceptanceAuditData>(auditLog.NewData!);
            return new ViewProjectAcceptanceTransitionDto
            {
                Action = auditLog.Action,
                CreatedDate = auditLog.CreatedDate,
                UserId = auditLog.UserId,
                UserName = auditLog.UserName,
                FullName = auditLog.FullName,
                OldStatus = oldData?.Status,
                NewStatus = newData?.Status,
                IsForced = newData?.IsForced ?? false,
                IsAutomatic = newData?.IsAutomatic ?? false,
                Reason = newData?.Reason,
            };
        }

        // Đếm "của tôi" trên văn bản mình đang giữ theo cặp, dùng chung paging và tiến trình của tôi ở chi tiết dự án
        private async Task<(Dictionary<(int ProjectId, int DocumentTypeId, string Role), PairCounts> MyCounts, Dictionary<(int ProjectId, int DocumentTypeId), int> MyFilledFieldCounts)>
            CountMyHeldSubmissionsAsync(List<int> projectIds, List<int> documentTypeIds, string userId)
        {
            // Số "của tôi" theo vai trò giữ: Data đếm văn bản đã kiểm, QA đếm văn bản đã chấm
            var myCounts = await (from u in _shDbContext.SubmissionUsers.AsNoTracking()
                                  join s in _shDbContext.Submissions on u.SubmissionId equals s.Id
                                  where u.UserId == userId && projectIds.Contains(u.ProjectId) && documentTypeIds.Contains(u.DocumentTypeId)
                                  group s by new { u.ProjectId, u.DocumentTypeId, u.Role } into g
                                  select new
                                  {
                                      g.Key.ProjectId,
                                      g.Key.DocumentTypeId,
                                      g.Key.Role,
                                      Counts = new PairCounts(
                                          g.Count(),
                                          g.Select(s => s.Box).Distinct().Count(),
                                          g.Select(s => s.Box + "|" + s.ProfileRange).Distinct().Count(),
                                          g.Count(s => g.Key.Role == RoleConstants.ROLE_QA
                                              ? s.StatusQa != SubmissionConst.QA_NOT_REVIEWED
                                              : s.StatusCheck == SubmissionConst.CHECKED),
                                          0),
                                  })
                                  .ToDictionaryAsync(c => (c.ProjectId, c.DocumentTypeId, c.Role), c => c.Counts);
            // Ô có dữ liệu chỉ tính trường tính tiền (IsChargeable) theo cờ hiện tại; query filter bỏ trường đã xoá
            var myFilledFieldCounts = await (from v in _shDbContext.SubmissionDatas.AsNoTracking()
                                             join u in _shDbContext.SubmissionUsers on v.SubmissionId equals u.SubmissionId
                                             join s in _shDbContext.Submissions on v.SubmissionId equals s.Id
                                             where u.UserId == userId && u.Role == RoleConstants.ROLE_DATA && s.StatusCheck == SubmissionConst.CHECKED
                                                 && v.Data.Trim() != String.Empty
                                                 && _shDbContext.DocumentFields.Any(f => f.Id == v.DocumentFieldId && f.IsChargeable)
                                                 && projectIds.Contains(v.ProjectId) && documentTypeIds.Contains(v.DocumentTypeId)
                                             group v by new { v.ProjectId, v.DocumentTypeId } into g
                                             select new { g.Key.ProjectId, g.Key.DocumentTypeId, Count = g.Count() })
                                             .ToDictionaryAsync(c => (c.ProjectId, c.DocumentTypeId), c => c.Count);
            return (myCounts, myFilledFieldCounts);
        }

        private static decimal CalculateQaPassedPercent(int totalQaPassed, int totalSubmissions)
        {
            return totalSubmissions == 0 ? 0 : Math.Round(totalQaPassed * 100m / totalSubmissions, 1);
        }

        private static readonly string[] AcceptanceActions =
        [
            AuditLogConstants.ACTION_PROJECT_ACCEPTANCE_START,
            AuditLogConstants.ACTION_PROJECT_ACCEPTANCE_FINISH,
            AuditLogConstants.ACTION_PROJECT_ACCEPTANCE_REOPEN,
        ];

        private record AcceptanceAuditData(int? Status, bool? IsForced, bool? IsAutomatic, string? Reason);

        private record PairCounts(int Total, int TotalBox, int TotalProfileRange, int Checked, int QaPassed);
    }
}
