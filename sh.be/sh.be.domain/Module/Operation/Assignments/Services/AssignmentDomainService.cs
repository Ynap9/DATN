using sh.be.domain.Module.Operation.Assignments.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;

namespace sh.be.domain.Module.Operation.Assignments.Services
{
    public class AssignmentDomainService
    {
        private readonly IAssignmentRepository _assignmentRepository;

        public AssignmentDomainService(IAssignmentRepository assignmentRepository)
        {
            _assignmentRepository = assignmentRepository;
        }

        public static void EnsureAssignableRole(string role)
        {
            if (role is not (RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
        }

        public static (string Box, string ProfileRange) ToNodeKey(string box, string profileRange)
        {
            return (box.Trim().ToUpperInvariant(), profileRange.Trim().ToUpperInvariant());
        }

        public static int CompareNatural(string? left, string? right)
        {
            left ??= String.Empty;
            right ??= String.Empty;
            var leftIndex = 0;
            var rightIndex = 0;
            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
                {
                    var leftStart = leftIndex;
                    var rightStart = rightIndex;
                    while (leftIndex < left.Length && char.IsDigit(left[leftIndex]))
                    {
                        leftIndex++;
                    }
                    while (rightIndex < right.Length && char.IsDigit(right[rightIndex]))
                    {
                        rightIndex++;
                    }
                    var leftNumber = left[leftStart..leftIndex].TrimStart('0');
                    var rightNumber = right[rightStart..rightIndex].TrimStart('0');
                    var numberResult = leftNumber.Length != rightNumber.Length
                        ? leftNumber.Length.CompareTo(rightNumber.Length)
                        : string.CompareOrdinal(leftNumber, rightNumber);
                    if (numberResult != 0)
                    {
                        return numberResult;
                    }
                    continue;
                }

                var charResult = char.ToUpperInvariant(left[leftIndex]).CompareTo(char.ToUpperInvariant(right[rightIndex]));
                if (charResult != 0)
                {
                    return charResult;
                }
                leftIndex++;
                rightIndex++;
            }
            return (left.Length - leftIndex).CompareTo(right.Length - rightIndex);
        }

        public static List<AssignmentItem> NormalizeItems(List<AssignmentItem> items)
        {
            var wholeBoxKeys = items
                .Where(item => item.ProfileRange == null)
                .Select(item => item.Box.Trim().ToUpperInvariant())
                .ToHashSet();
            HashSet<(string, string?)> seenKeys = [];
            List<AssignmentItem> normalizedItems = [];
            foreach (var item in items)
            {
                var box = item.Box.Trim();
                var profileRange = item.ProfileRange?.Trim();
                if (profileRange != null && wholeBoxKeys.Contains(box.ToUpperInvariant()))
                {
                    continue;
                }
                if (seenKeys.Add((box.ToUpperInvariant(), profileRange?.ToUpperInvariant())))
                {
                    normalizedItems.Add(new AssignmentItem(box, profileRange));
                }
            }
            if (normalizedItems.Count > AssignmentConst.ITEMS_PER_REQUEST_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Mỗi lần giao tối đa {AssignmentConst.ITEMS_PER_REQUEST_MAX} hộp / hồ sơ");
            }
            return normalizedItems;
        }

        public static AssignmentPlan BuildDataPlan(string assigneeUserId, List<AssignmentNode> nodes, List<AssignmentItem> emptyItems,
            List<AssignmentSubmissionRow> rows, HashSet<int> returnedToSameIds)
        {
            var newRows = rows.Where(row => row.DataHolderUserId == null).ToList();
            var conflictRows = rows.Where(row => row.DataHolderUserId != null && row.DataHolderUserId != assigneeUserId).ToList();
            return new AssignmentPlan
            {
                Role = RoleConstants.ROLE_DATA,
                AssigneeUserId = assigneeUserId,
                Nodes = nodes,
                EmptyItems = emptyItems,
                TotalDocuments = rows.Count,
                NewSubmissionIds = newRows.Select(row => row.SubmissionId).ToList(),
                AlreadyHeldDocuments = rows.Count(row => row.DataHolderUserId == assigneeUserId),
                ConflictDocuments = conflictRows.Count,
                ReturnedToSameDocuments = newRows.Count(row => returnedToSameIds.Contains(row.SubmissionId)),
                Conflicts = conflictRows
                    .GroupBy(row => (ToNodeKey(row.Box, row.ProfileRange), row.DataHolderUserId!))
                    .Select(group => new AssignmentConflict(group.First().Box, group.First().ProfileRange, group.Key.Item2, group.Count(), false))
                    .ToList(),
            };
        }

        public static AssignmentPlan BuildQaPlan(string assigneeUserId, List<AssignmentNode> nodes, List<AssignmentItem> emptyItems,
            List<AssignmentSubmissionRow> rows, List<QaOwnership> ownerships, HashSet<int> sameAsEntryIds, bool isSampleMode)
        {
            var ownerByNode = ownerships
                .GroupBy(ownership => ToNodeKey(ownership.Box, ownership.ProfileRange))
                .ToDictionary(group => group.Key, group => group.First());
            var rowsByNode = rows.ToLookup(row => ToNodeKey(row.Box, row.ProfileRange));

            List<AssignmentConflict> conflicts = [];
            List<AssignmentNode> newOwnedNodes = [];
            foreach (var node in nodes)
            {
                var nodeKey = ToNodeKey(node.Box, node.ProfileRange);
                if (!ownerByNode.TryGetValue(nodeKey, out var owner))
                {
                    newOwnedNodes.Add(node);
                }
                else if (owner.QaUserId != assigneeUserId)
                {
                    conflicts.Add(new AssignmentConflict(node.Box, node.ProfileRange, owner.QaUserId, rowsByNode[nodeKey].Count(), true));
                }
            }

            var conflictRows = rows
                .Where(row => row.QaHolderUserId != null && row.QaHolderUserId != assigneeUserId && row.StatusQa != SubmissionConst.QA_PASSED)
                .ToList();
            conflicts.AddRange(conflictRows
                .GroupBy(row => (ToNodeKey(row.Box, row.ProfileRange), row.QaHolderUserId!))
                .Select(group => new AssignmentConflict(group.First().Box, group.First().ProfileRange, group.Key.Item2, group.Count(), false)));

            var freeRows = rows.Where(row => row.QaHolderUserId == null).ToList();
            return new AssignmentPlan
            {
                Role = RoleConstants.ROLE_QA,
                AssigneeUserId = assigneeUserId,
                Nodes = nodes,
                EmptyItems = emptyItems,
                TotalDocuments = rows.Count,
                NewSubmissionIds = freeRows
                    .Where(row => row.StatusCheck == SubmissionConst.CHECKED && row.StatusQa == SubmissionConst.QA_NOT_REVIEWED)
                    .Select(row => row.SubmissionId)
                    .ToList(),
                AlreadyHeldDocuments = rows.Count(row => row.QaHolderUserId == assigneeUserId),
                ConflictDocuments = conflictRows.Count,
                LaterDocuments = freeRows.Count(row => row.StatusCheck != SubmissionConst.CHECKED),
                SameAsEntryDocuments = rows.Count(row => sameAsEntryIds.Contains(row.SubmissionId)),
                Conflicts = conflicts,
                NewOwnedNodes = newOwnedNodes,
                OwnershipTicketId = ownerships.FirstOrDefault(ownership => ownership.QaUserId == assigneeUserId)?.TicketId,
                IsSampleMode = isSampleMode,
            };
        }

        public static List<UserFriendlyException> ResolveBlockErrors(AssignmentPlan plan)
        {
            List<UserFriendlyException> errors = [];
            if (plan.EmptyItems.Count > 0)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.OperationErrorAssignmentNodeEmpty,
                    $"Hộp / hồ sơ không có văn bản để giao: {string.Join(", ", plan.EmptyItems.Select(item => item.ProfileRange == null ? item.Box : $"{item.Box} / {item.ProfileRange}"))}"));
            }
            if (plan.Conflicts.Count > 0)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict));
            }
            if (plan.SameAsEntryDocuments > 0)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.ConstructionErrorQaSameAsEntry));
            }
            if (plan.IsSampleMode)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.BadRequest, "Chưa hỗ trợ giao phiếu chế độ SAMPLE"));
            }

            var isNothingToAssign = plan.Role == RoleConstants.ROLE_DATA
                ? plan.NewSubmissionIds.Count == 0
                : plan.NewSubmissionIds.Count == 0 && (plan.LaterDocuments == 0 || plan.NewOwnedNodes.Count == 0);
            if (isNothingToAssign)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.OperationErrorNothingToAssign));
            }
            if (plan.Role == RoleConstants.ROLE_DATA && plan.NewSubmissionIds.Count > AssignmentConst.DATA_TICKET_MAX_DOCUMENTS)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.OperationErrorTicketTooLarge));
            }
            if (plan.Role == RoleConstants.ROLE_QA && plan.NewSubmissionIds.Count > QaAssignmentConst.TICKET_MAX_DOCUMENTS)
            {
                errors.Add(new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketTooLarge));
            }
            return errors;
        }

        public static void EnsurePlanSavable(AssignmentPlan plan)
        {
            var firstError = ResolveBlockErrors(plan).FirstOrDefault();
            if (firstError != null)
            {
                throw firstError;
            }
        }

        public async Task<AssignmentPlan> PlanAsync(OperationAccess access, string role, string assigneeUserId, List<AssignmentItem> items)
        {
            EnsureAssignableRole(role);
            var projectId = access.Project.Id;
            var documentTypeId = access.DocumentType.Id;
            var member = await _assignmentRepository.FindActiveMemberAsync(projectId, documentTypeId, assigneeUserId, role)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorAssigneeNotMember);
            if (!access.IsAdmin && !member.IsManagedBy(access.LeaderUserId!))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderForbiddenTarget);
            }

            var normalizedItems = NormalizeItems(items);
            var boxes = normalizedItems.Select(item => item.Box).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var boxRows = await _assignmentRepository.GetSubmissionRowsAsync(projectId, documentTypeId, boxes);

            List<AssignmentNode> nodes = [];
            List<AssignmentItem> emptyItems = [];
            HashSet<(string, string)> nodeKeys = [];
            foreach (var item in normalizedItems)
            {
                var itemRows = boxRows
                    .Where(row => string.Equals(row.Box, item.Box, StringComparison.OrdinalIgnoreCase)
                        && (item.ProfileRange == null || string.Equals(row.ProfileRange, item.ProfileRange, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (itemRows.Count == 0)
                {
                    emptyItems.Add(item);
                    continue;
                }
                nodes.AddRange(itemRows
                    .Where(row => nodeKeys.Add(ToNodeKey(row.Box, row.ProfileRange)))
                    .Select(row => new AssignmentNode(row.Box, row.ProfileRange)));
            }
            var rows = boxRows.Where(row => nodeKeys.Contains(ToNodeKey(row.Box, row.ProfileRange))).ToList();

            if (role == RoleConstants.ROLE_DATA)
            {
                var returnedFreeIds = rows
                    .Where(row => row.DataHolderUserId == null && row.StatusCheck == SubmissionConst.RETURNED)
                    .Select(row => row.SubmissionId)
                    .ToList();
                var returnedToSameIds = await _assignmentRepository.GetSubmissionIdsCheckedByAsync(returnedFreeIds, assigneeUserId);
                return BuildDataPlan(assigneeUserId, nodes, emptyItems, rows, returnedToSameIds);
            }

            var sameAsEntryIds = await _assignmentRepository.GetSubmissionIdsCheckedByAsync(rows.Select(row => row.SubmissionId).ToList(), assigneeUserId);
            sameAsEntryIds.UnionWith(rows.Where(row => row.DataHolderUserId == assigneeUserId).Select(row => row.SubmissionId));
            var ownerships = await _assignmentRepository.GetActiveQaOwnershipsAsync(projectId, documentTypeId);
            var reviewerMode = await _assignmentRepository.FindQaReviewerModeAsync(projectId, assigneeUserId);
            return BuildQaPlan(assigneeUserId, nodes, emptyItems, rows, ownerships, sameAsEntryIds, reviewerMode == QaAssignmentConst.MODE_SAMPLE);
        }
    }

    public record AssignmentItem(string Box, string? ProfileRange);

    public record AssignmentNode(string Box, string ProfileRange);

    public record AssignmentSubmissionRow(int SubmissionId, string Box, string ProfileRange, int StatusCheck, int StatusQa,
        string? DataHolderUserId, string? QaHolderUserId);

    public record QaOwnership(int TicketId, string QaUserId, string Box, string ProfileRange);

    public record AssignmentConflict(string Box, string ProfileRange, string HolderUserId, int DocumentCount, bool IsOwner);

    public record AssignmentPlan
    {
        public string Role { get; init; } = String.Empty;
        public string AssigneeUserId { get; init; } = String.Empty;
        public List<AssignmentNode> Nodes { get; init; } = [];
        public List<AssignmentItem> EmptyItems { get; init; } = [];
        public int TotalDocuments { get; init; }
        public List<int> NewSubmissionIds { get; init; } = [];
        public int AlreadyHeldDocuments { get; init; }
        public int ConflictDocuments { get; init; }
        public int LaterDocuments { get; init; }
        public int SameAsEntryDocuments { get; init; }
        public int ReturnedToSameDocuments { get; init; }
        public List<AssignmentConflict> Conflicts { get; init; } = [];
        public List<AssignmentNode> NewOwnedNodes { get; init; } = [];
        public int? OwnershipTicketId { get; init; }
        public bool IsSampleMode { get; init; }
    }
}
