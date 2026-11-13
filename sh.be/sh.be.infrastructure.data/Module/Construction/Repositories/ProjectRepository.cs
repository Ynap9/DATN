using Microsoft.EntityFrameworkCore;
using sh.be.domain.Module.Ai.ProjectConfigs.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;
using sh.be.domain.Module.Construction.Projects.Repositories;
using sh.be.domain.Module.Construction.Projects.Services;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Interfaces;

namespace sh.be.infrastructure.data.Module.Construction.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private const string TryLockDeadlineCheckSql =
            "DECLARE @result int; "
            + "EXEC @result = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; "
            + "SELECT @result AS [Value];";
        private const string ReleaseDeadlineCheckLockSql = "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = 'Session';";

        private readonly ShDbContext _shDbContext;

        public ProjectRepository(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public Task<Project?> FindByIdAsync(int id)
        {
            return _shDbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null)
        {
            return _shDbContext.Projects.AnyAsync(p => p.Name == name && p.Id != excludedId);
        }

        public Task<List<Project>> GetWithEndedAtByStatusesAsync(IReadOnlyCollection<int> statuses)
        {
            return _shDbContext.Projects
                .Where(p => statuses.Contains(p.Status) && p.EndedAt != null)
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<bool> TryLockDeadlineCheckAsync()
        {
            await _shDbContext.Database.OpenConnectionAsync();
            var lockResults = await _shDbContext.Database
                .SqlQueryRaw<int>(TryLockDeadlineCheckSql, ProjectDeadlineConst.LOCK_NAME)
                .ToListAsync();
            var isLocked = lockResults[0] >= 0;
            if (!isLocked)
            {
                await _shDbContext.Database.CloseConnectionAsync();
            }
            return isLocked;
        }

        public async Task ReleaseDeadlineCheckLockAsync()
        {
            await _shDbContext.Database.ExecuteSqlRawAsync(ReleaseDeadlineCheckLockSql, ProjectDeadlineConst.LOCK_NAME);
            await _shDbContext.Database.CloseConnectionAsync();
        }

        public Task LockAsync(int id)
        {
            return _shDbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT Id FROM [construction].[Project] WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}");
        }

        public Task<bool> IsActiveLeaderAsync(int projectId, string userId)
        {
            return _shDbContext.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId
                && m.Role == RoleConstants.ROLE_LEADER && m.IsActive);
        }

        // Vai trò của người dùng ở mọi loại văn bản của dự án, kể cả đang tắt; người đã gỡ bị query filter loại
        public Task<List<string>> GetMemberRolesAsync(int projectId, string userId)
        {
            return _shDbContext.ProjectMembers
                .AsNoTracking()
                .Where(m => m.ProjectId == projectId && m.UserId == userId)
                .Select(m => m.Role)
                .Distinct()
                .ToListAsync();
        }

        // Đếm văn bản chưa kiểm / chưa QA đạt / tổng của dự án bằng một câu GROUP BY, kèm cờ QA để tính đủ điều kiện nghiệm thu
        public async Task<ProjectAcceptanceBlockers> CountAcceptanceBlockersAsync(int projectId, bool isQaEnabled)
        {
            return await _shDbContext.Submissions
                .Where(s => s.ProjectId == projectId)
                .GroupBy(s => s.ProjectId)
                .Select(g => new ProjectAcceptanceBlockers(
                    g.Count(s => s.StatusCheck != SubmissionConst.CHECKED),
                    g.Count(s => s.StatusCheck == SubmissionConst.CHECKED && s.StatusQa != SubmissionConst.QA_PASSED),
                    g.Count(),
                    isQaEnabled))
                .FirstOrDefaultAsync()
                ?? new ProjectAcceptanceBlockers(0, 0, 0, isQaEnabled);
        }

        public Task<int> TryStartAcceptanceAsync(int projectId)
        {
            return _shDbContext.Projects
                .Where(p => p.Id == projectId && ProjectConst.ACCEPTANCE_START_STATUSES.Contains(p.Status))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, ProjectConst.ACCEPTING));
        }

        // Xoá mềm mọi dữ liệu phụ thuộc dự án, bảng con trước, gồm bảng Operation / Ai (cùng ShDbContext, cùng transaction người gọi); trả số dòng từng bảng
        public async Task<Dictionary<string, int>> SoftDeleteDependentsAsync(int projectId, string? deletedBy, DateTime deletedDate)
        {
            return new Dictionary<string, int>
            {
                [nameof(SubmissionQaLogField)] = await SoftDeleteAsync(_shDbContext.SubmissionQaLogFields
                    .Where(f => _shDbContext.SubmissionQaLogs.Any(l => l.Id == f.SubmissionQaLogId && l.ProjectId == projectId)), deletedBy, deletedDate),
                [nameof(SubmissionQaLog)] = await SoftDeleteAsync(_shDbContext.SubmissionQaLogs.Where(l => l.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(SubmissionLog)] = await SoftDeleteAsync(_shDbContext.SubmissionLogs.Where(l => l.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(SubmissionData)] = await SoftDeleteAsync(_shDbContext.SubmissionDatas.Where(d => d.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(SubmissionUser)] = await SoftDeleteAsync(_shDbContext.SubmissionUsers.Where(u => u.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(Submission)] = await SoftDeleteAsync(_shDbContext.Submissions.Where(s => s.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(QaTicket)] = await SoftDeleteAsync(_shDbContext.QaTickets.Where(t => t.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(QaReviewerMode)] = await SoftDeleteAsync(_shDbContext.QaReviewerModes.Where(m => m.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(QaSamplingConfig)] = await SoftDeleteAsync(_shDbContext.QaSamplingConfigs.Where(c => c.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(AssignmentTicketItem)] = await SoftDeleteAsync(_shDbContext.AssignmentTicketItems
                    .Where(i => _shDbContext.AssignmentTickets.Any(t => t.Id == i.AssignmentTicketId && t.ProjectId == projectId)), deletedBy, deletedDate),
                [nameof(AssignmentTicket)] = await SoftDeleteAsync(_shDbContext.AssignmentTickets.Where(t => t.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(ProjectMember)] = await SoftDeleteAsync(_shDbContext.ProjectMembers.Where(m => m.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(ProjectAiConfig)] = await SoftDeleteAsync(_shDbContext.ProjectAiConfigs.Where(c => c.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(ProjectDeadlineNotification)] = await SoftDeleteAsync(_shDbContext.ProjectDeadlineNotifications.Where(n => n.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(ProjectDocumentTypeDocumentField)] = await SoftDeleteAsync(_shDbContext.ProjectDocumentTypeDocumentFields.Where(pf => pf.ProjectId == projectId), deletedBy, deletedDate),
                [nameof(ProjectDocumentType)] = await SoftDeleteAsync(_shDbContext.ProjectDocumentTypes.Where(pd => pd.ProjectId == projectId), deletedBy, deletedDate),
            };
        }

        public void Add(Project project)
        {
            _shDbContext.Projects.Add(project);
        }

        public void Update(Project project)
        {
            _shDbContext.Projects.Update(project);
        }

        public Task<List<ProjectDocumentType>> GetDocumentTypesAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypes
                .Where(pd => pd.ProjectId == projectId)
                .ToListAsync();
        }

        public Task<List<int>> GetDocumentTypeIdsAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypes
                .Where(pd => pd.ProjectId == projectId)
                .Select(pd => pd.DocumentTypeId)
                .ToListAsync();
        }

        public Task<bool> ExistsDocumentTypeAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.ProjectId == projectId && pd.DocumentTypeId == documentTypeId);
        }

        public Task<bool> ExistsActiveDocumentTypeAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypes.AnyAsync(pd => pd.ProjectId == projectId && pd.DocumentTypeId == documentTypeId
                && _shDbContext.Projects.Any(p => p.Id == projectId)
                && _shDbContext.DocumentTypes.Any(d => d.Id == documentTypeId));
        }

        public void AddDocumentTypes(IEnumerable<ProjectDocumentType> projectDocumentTypes)
        {
            _shDbContext.ProjectDocumentTypes.AddRange(projectDocumentTypes);
        }

        public Task DeleteDocumentTypesAsync(int projectId, IReadOnlyCollection<int> documentTypeIds)
        {
            return _shDbContext.ProjectDocumentTypes
                .IgnoreQueryFilters()
                .Where(pd => pd.ProjectId == projectId && documentTypeIds.Contains(pd.DocumentTypeId))
                .ExecuteDeleteAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .Where(pf => pf.ProjectId == projectId)
                .ToListAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsAsync(int projectId, int documentTypeId)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .AsNoTracking()
                .Where(pf => pf.ProjectId == projectId && pf.DocumentTypeId == documentTypeId)
                .ToListAsync();
        }

        public Task<List<ProjectDocumentTypeDocumentField>> GetDocumentFieldsByDocumentFieldIdsAsync(IReadOnlyCollection<int> documentFieldIds)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .Where(pf => documentFieldIds.Contains(pf.DocumentFieldId))
                .ToListAsync();
        }

        public void AddDocumentFields(IEnumerable<ProjectDocumentTypeDocumentField> projectDocumentFields)
        {
            _shDbContext.ProjectDocumentTypeDocumentFields.AddRange(projectDocumentFields);
        }

        public Task DeleteDocumentFieldsAsync(int projectId, IReadOnlyCollection<int> documentTypeIds)
        {
            return _shDbContext.ProjectDocumentTypeDocumentFields
                .IgnoreQueryFilters()
                .Where(pf => pf.ProjectId == projectId && documentTypeIds.Contains(pf.DocumentTypeId))
                .ExecuteDeleteAsync();
        }

        // Đánh dấu xoá các dòng chưa xoá (query filter bỏ dòng đã xoá từ trước, giữ mốc xoá cũ) với cùng người và mốc xoá
        private static Task<int> SoftDeleteAsync<TEntity>(IQueryable<TEntity> query, string? deletedBy, DateTime deletedDate)
            where TEntity : class, ISoftDeleted
        {
            return query.ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Deleted, true)
                .SetProperty(e => e.DeletedBy, deletedBy)
                .SetProperty(e => e.DeletedDate, deletedDate));
        }
    }
}
