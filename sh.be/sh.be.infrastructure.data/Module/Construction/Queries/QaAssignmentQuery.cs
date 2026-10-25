using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Construction.Dtos.QaAssignment;
using sh.be.applications.Module.Construction.Interfaces.Queries;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;

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
    }
}
