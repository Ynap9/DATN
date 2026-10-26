using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Construction.QaAssignments.Entities;
using sh.be.domain.Module.Construction.QaAssignments.Models;
using sh.be.domain.Module.Construction.QaAssignments.Repositories;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public class QaAssignmentDomainService
    {
        private readonly IQaSamplingConfigRepository _qaSamplingConfigRepository;
        private readonly IQaReviewerModeRepository _qaReviewerModeRepository;
        private readonly IQaTicketRepository _qaTicketRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public QaAssignmentDomainService(IQaSamplingConfigRepository qaSamplingConfigRepository, IQaReviewerModeRepository qaReviewerModeRepository,
            IQaTicketRepository qaTicketRepository, IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _qaSamplingConfigRepository = qaSamplingConfigRepository;
            _qaReviewerModeRepository = qaReviewerModeRepository;
            _qaTicketRepository = qaTicketRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<QaSamplingConfig> GetConfigOrDefaultAsync(int projectId)
        {
            return await _qaSamplingConfigRepository.FindByProjectAsync(projectId) ?? QaSamplingConfig.Default(projectId);
        }

        public async Task<string> GetModeAsync(int projectId, string userId)
        {
            var reviewerMode = await _qaReviewerModeRepository.FindAsync(projectId, userId);
            return reviewerMode?.Mode ?? QaAssignmentConst.MODE_FULL;
        }

        public async Task EnsureAssignableModeAsync(int projectId, string qaUserId)
        {
            if (await GetModeAsync(projectId, qaUserId) == QaAssignmentConst.MODE_SAMPLE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chưa hỗ trợ giao phiếu chế độ SAMPLE");
            }
        }

        public async Task<AppUser> EnsureQaReviewerAsync(string userId)
        {
            var user = await _userRepository.FindByIdAsync(userId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorUserNotFound);
            var roleNames = await _roleRepository.GetRoleNamesByUserIdAsync(user.Id);
            if (!roleNames.Contains(RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Người dùng không có vai trò QA");
            }
            return user;
        }

        public async Task<QaAccess> GetAccessAsync(string userId)
        {
            var isAdmin = QaAccessPolicy.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(userId));
            List<(int ProjectId, int DocumentTypeId)> managedPairs = isAdmin ? [] : await _qaTicketRepository.GetLeaderPairsAsync(userId);
            return new QaAccess(isAdmin, managedPairs.ToHashSet());
        }

        public void EnsureManagesPair(QaAccess access, int projectId, int documentTypeId)
        {
            if (!access.IsAdmin && !access.ManagedPairs.Contains((projectId, documentTypeId)))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorProjectNotManaged);
            }
        }

        public bool CanSeeTicket(QaAccess access, QaTicket ticket, string userId)
        {
            return access.IsAdmin || ticket.IsAssignedTo(userId) || access.ManagedPairs.Contains((ticket.ProjectId, ticket.DocumentTypeId));
        }

        public async Task<QaTicket> EnsureVisibleTicketAsync(int ticketId, string userId)
        {
            var ticket = await _qaTicketRepository.FindByIdAsync(ticketId)
                ?? throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketNotFound);
            if (!CanSeeTicket(await GetAccessAsync(userId), ticket, userId))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketNotFound);
            }
            return ticket;
        }

        public async Task<QaTicket?> EnsureReviewableAsync(Submission submission, string userId, bool isAdmin)
        {
            var ticket = await _qaTicketRepository.FindBySubmissionAsync(submission.Id);
            if (isAdmin)
            {
                return ticket;
            }
            if (ticket == null)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaNotAssignee);
            }
            ticket.EnsureReviewableBy(userId, submission.StatusQa);
            return ticket;
        }

        public async Task<bool> TryPassFullTicketAsync(QaTicket ticket, string? passedBy, DateTime passedDate)
        {
            if (!ticket.IsActiveFull() || await _qaTicketRepository.CountNotPassedAsync(ticket.Id) > 0)
            {
                return false;
            }
            if (await _qaTicketRepository.TryConcludeAsync(ticket.Id, QaAssignmentConst.TICKET_STATUS_PASSED) == 0)
            {
                return false;
            }
            ticket.Pass(passedBy, passedDate);
            return true;
        }

        public async Task<List<QaCandidate>> SelectAssignableAsync(int projectId, int documentTypeId, string qaUserId,
            IReadOnlyCollection<QaTicketNode>? nodes, int? count)
        {
            var isByNodes = nodes is { Count: > 0 };
            if (isByNodes == count.HasValue)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chọn đúng một cách giao: theo hộp / hồ sơ hoặc theo số lượng");
            }
            if (count is < 1 or > QaAssignmentConst.TICKET_MAX_DOCUMENTS)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Số lượng phải từ 1 đến {QaAssignmentConst.TICKET_MAX_DOCUMENTS}");
            }
            if (isByNodes && nodes!.Any(node => string.IsNullOrWhiteSpace(node.Box)))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Hồ sơ phải đi kèm hộp");
            }

            var candidates = await _qaTicketRepository.GetCandidatesAsync(projectId, documentTypeId);
            var selected = isByNodes
                ? candidates.Where(candidate => nodes!.Any(node => node.Contains(candidate))).ToList()
                : candidates.Take(count!.Value).ToList();
            if (selected.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaNoSubmissionToAssign);
            }
            if (selected.Count > QaAssignmentConst.TICKET_MAX_DOCUMENTS)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketTooLarge);
            }
            if (await _qaTicketRepository.AnyEnteredByAsync(selected.Select(candidate => candidate.SubmissionId).ToList(), qaUserId))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaSameAsEntry);
            }
            return selected;
        }
    }
}
