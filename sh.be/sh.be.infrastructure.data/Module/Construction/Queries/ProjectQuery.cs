using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.Project;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
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

        public Task<List<GetListProjectDto>> GetList()
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .ProjectTo<GetListProjectDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public Task<List<GetListProjectDropDownDto>> GetProjectDropDown()
        {
            return _shDbContext.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ProjectTo<GetListProjectDropDownDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListProjectDto>> FindPaging(FindPagingProjectDto dto)
        {
            var query = _shDbContext.Projects.AsNoTracking();
            if (dto.Status.HasValue)
            {
                query = query.Where(p => p.Status == dto.Status);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(p => p.Name.Contains(dto.Keyword));
            }

            var totalCount = await query.CountAsync();

            var projects = await query
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Paging(dto)
                .ProjectTo<GetListProjectDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new BaseResponsePagingDto<GetListProjectDto>
            {
                Items = projects,
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
    }
}
