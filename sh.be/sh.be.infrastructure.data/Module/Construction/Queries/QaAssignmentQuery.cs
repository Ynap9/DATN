using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Construction.Queries
{
    public class QaAssignmentQuery : BaseQuery, IQaAssignmentQuery
    {
        public QaAssignmentQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<List<GetListQaReviewerModeDto>> FindReviewerModes(int projectId)
        {
            return (from m in _shDbContext.QaReviewerModes.AsNoTracking()
                    join u in _shDbContext.Users.AsNoTracking() on m.UserId equals u.Id into users
                    from u in users.DefaultIfEmpty()
                    where m.ProjectId == projectId
                    orderby u.UserName, m.UserId
                    select new GetListQaReviewerModeDto
                    {
                        UserId = m.UserId,
                        UserName = u.UserName,
                        FullName = u.FullName,
                        Mode = m.Mode,
                        ModifiedDate = m.ModifiedDate ?? m.CreatedDate,
                    })
                .ToListAsync();
        }

        public async Task<BaseResponsePagingDto<GetListQaTicketDto>> FindTicketPaging(FindPagingQaTicketDto dto, string? viewerUserId)
        {
            var query = _shDbContext.QaTickets.AsNoTracking();
            if (viewerUserId != null)
            {
                query = query.Where(t => t.QaUserId == viewerUserId
                    || _shDbContext.ProjectMembers.Any(m => m.UserId == viewerUserId && m.Role == RoleConstants.ROLE_LEADER && m.IsActive
                        && m.ProjectId == t.ProjectId && m.DocumentTypeId == t.DocumentTypeId));
            }
            if (dto.ProjectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == dto.ProjectId.Value);
            }
            if (dto.DocumentTypeId.HasValue)
            {
                query = query.Where(t => t.DocumentTypeId == dto.DocumentTypeId.Value);
            }
            if (!string.IsNullOrEmpty(dto.QaUserId))
            {
                query = query.Where(t => t.QaUserId == dto.QaUserId);
            }
            if (!string.IsNullOrEmpty(dto.Mode))
            {
                query = query.Where(t => t.Mode == dto.Mode);
            }
            if (!string.IsNullOrEmpty(dto.Status))
            {
                query = query.Where(t => t.Status == dto.Status);
            }

            var totalCount = await query.CountAsync();

            var tickets = await query
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.Id)
                .Paging(dto)
                .Select(t => new GetListQaTicketDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    DocumentTypeId = t.DocumentTypeId,
                    QaUserId = t.QaUserId,
                    QaUserName = _shDbContext.Users.Where(u => u.Id == t.QaUserId).Select(u => u.UserName).FirstOrDefault(),
                    Mode = t.Mode,
                    DocumentCount = t.DocumentCount,
                    PassedCount = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                        && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa == SubmissionConst.QA_PASSED)),
                    SampleSize = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.SampleSize : null,
                    MaxFailed = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.MaxFailed : null,
                    Round = t.Round,
                    RejectedCount = t.RejectedCount,
                    ReviewedCount = t.ReviewedCount,
                    FailedCount = t.FailedCount,
                    Status = t.Status,
                    CreatedDate = t.CreatedDate,
                    ConcludedDate = t.ConcludedDate,
                })
                .ToListAsync();

            return new BaseResponsePagingDto<GetListQaTicketDto>
            {
                Items = tickets,
                TotalItems = totalCount,
            };
        }

        public async Task<FindQaTicketByIdDto?> FindTicketById(int id)
        {
            var ticket = await _shDbContext.QaTickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new FindQaTicketByIdDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    DocumentTypeId = t.DocumentTypeId,
                    QaUserId = t.QaUserId,
                    QaUserName = _shDbContext.Users.Where(u => u.Id == t.QaUserId).Select(u => u.UserName).FirstOrDefault(),
                    Mode = t.Mode,
                    DocumentCount = t.DocumentCount,
                    PassedCount = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                        && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa == SubmissionConst.QA_PASSED)),
                    SampleSize = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.SampleSize : null,
                    MaxFailed = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.MaxFailed : null,
                    Round = t.Round,
                    RejectedCount = t.RejectedCount,
                    ReviewedCount = t.ReviewedCount,
                    FailedCount = t.FailedCount,
                    Status = t.Status,
                    CreatedDate = t.CreatedDate,
                    ConcludedDate = t.ConcludedDate,
                    ThresholdPercent = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.ThresholdPercent : null,
                    StrategyCode = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.StrategyCode : null,
                    StrategyParamsJson = t.StrategyParamsJson,
                    Seed = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.Seed : null,
                    StopEarlyOnReject = t.Mode == QaAssignmentConst.MODE_SAMPLE ? t.StopEarlyOnReject : null,
                    RoundStartedDate = t.RoundStartedDate,
                })
                .FirstOrDefaultAsync();
            if (ticket == null)
            {
                return null;
            }

            var roundStartedDate = ticket.RoundStartedDate;
            ticket.Samples = await (from u in _shDbContext.SubmissionUsers.AsNoTracking()
                                    join s in _shDbContext.Submissions.AsNoTracking() on u.SubmissionId equals s.Id
                                    where u.TicketId == id && u.Role == RoleConstants.ROLE_QA && u.IsSample
                                    orderby s.Box, s.ProfileRange, s.Id
                                    select new GetListQaTicketSampleDto
                                    {
                                        SubmissionId = s.Id,
                                        FileName = s.FileName,
                                        Box = s.Box,
                                        ProfileRange = s.ProfileRange,
                                        FirstResult = _shDbContext.SubmissionQaLogs
                                            .Where(l => l.SubmissionId == s.Id && l.CreatedDate >= roundStartedDate)
                                            .OrderBy(l => l.CreatedDate)
                                            .ThenBy(l => l.Id)
                                            .Select(l => (int?)l.NewStatusQa)
                                            .FirstOrDefault(),
                                        StatusCheck = s.StatusCheck,
                                        StatusQa = s.StatusQa,
                                    })
                .ToListAsync();
            return ticket;
        }
    }
}
