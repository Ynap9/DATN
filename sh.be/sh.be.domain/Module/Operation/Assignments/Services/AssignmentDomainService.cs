using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Construction.Submissions.Entities;
using sh.be.domain.Module.Operation.Assignments.Entities;
using sh.be.domain.Module.Operation.Assignments.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
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
        private readonly IRoleRepository _roleRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;

        public AssignmentDomainService(IAssignmentRepository assignmentRepository, IRoleRepository roleRepository,
            IProjectMemberRepository projectMemberRepository)
        {
            _assignmentRepository = assignmentRepository;
            _roleRepository = roleRepository;
            _projectMemberRepository = projectMemberRepository;
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
            ProjectMemberDomainService.EnsureRoleEnabled(access.Project, role);
            var projectId = access.Project.Id;
            var documentTypeId = access.DocumentType.Id;
            await EnsureAssigneeAsync(access, assigneeUserId, role);

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

        public async Task<string?> ResolveRestrictedUserIdAsync(int projectId, int documentTypeId, string userId)
        {
            if (PermissionDomainService.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(userId)))
            {
                return null;
            }
            if ((await _roleRepository.GetPermissionKeysByUserIdAsync(userId)).Contains(PermissionConstants.FUNCTION_ASSIGNMENT_VIEW_ALL_SUBMISSION)
                && await _projectMemberRepository.FindActiveLeaderAsync(projectId, documentTypeId, userId) != null)
            {
                return null;
            }
            if (!await _assignmentRepository.IsMemberAsync(projectId, documentTypeId, userId))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorSubmissionNotAssigned);
            }
            return userId;
        }

        public async Task EnsureCanViewAsync(Submission submission, string userId)
        {
            if (await ResolveRestrictedUserIdAsync(submission.ProjectId, submission.DocumentTypeId, userId) == null)
            {
                return;
            }
            var dataHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_DATA);
            var qaHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_QA);
            if (dataHolder?.UserId != userId && qaHolder?.UserId != userId)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorSubmissionNotAssigned);
            }
        }

        public async Task EnsureCanCheckAsync(Submission submission, string userId)
        {
            if (await ResolveRestrictedUserIdAsync(submission.ProjectId, submission.DocumentTypeId, userId) == null)
            {
                return;
            }
            var dataHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_DATA);
            if (dataHolder?.UserId != userId)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorSubmissionNotAssigned);
            }
            if (submission.StatusQa == SubmissionConst.QA_PASSED)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorSubmissionQaPassedLocked);
            }
        }

        public async Task<QaOwnership?> ResolveQaOwnerForCheckedAsync(Submission submission)
        {
            if (submission.StatusQa != SubmissionConst.QA_NOT_REVIEWED
                || await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_QA) != null)
            {
                return null;
            }
            var nodeKey = ToNodeKey(submission.Box, submission.ProfileRange);
            var owner = (await _assignmentRepository.GetActiveQaOwnershipsAsync(submission.ProjectId, submission.DocumentTypeId))
                .FirstOrDefault(ownership => ToNodeKey(ownership.Box, ownership.ProfileRange) == nodeKey);
            if (owner == null
                || await _assignmentRepository.FindActiveMemberAsync(submission.ProjectId, submission.DocumentTypeId, owner.QaUserId, RoleConstants.ROLE_QA) == null)
            {
                return null;
            }
            var dataHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_DATA);
            if (dataHolder?.UserId == owner.QaUserId
                || (await _assignmentRepository.GetSubmissionIdsCheckedByAsync([submission.Id], owner.QaUserId)).Count > 0
                || await _assignmentRepository.FindQaReviewerModeAsync(submission.ProjectId, owner.QaUserId) == QaAssignmentConst.MODE_SAMPLE)
            {
                return null;
            }

            await _assignmentRepository.LockPairAsync(submission.ProjectId, submission.DocumentTypeId);
            return await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_QA) == null ? owner : null;
        }

        public async Task<QaResultOnEntry?> ApplyQaResultAsync(Submission submission, int newStatusQa, string? modifiedBy, DateTime now)
        {
            var dataHolder = await _assignmentRepository.FindHolderAsync(submission.Id, RoleConstants.ROLE_DATA);
            if (dataHolder == null)
            {
                return null;
            }
            if (newStatusQa != SubmissionConst.QA_FAILED)
            {
                return new QaResultOnEntry(await _assignmentRepository.FindTicketByIdAsync(dataHolder.TicketId)
                    ?? throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound), dataHolder.UserId, null);
            }

            await _assignmentRepository.LockPairAsync(submission.ProjectId, submission.DocumentTypeId);
            var ticket = await _assignmentRepository.FindTicketByIdAsync(dataHolder.TicketId)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
            string? releaseReason = null;
            if (ticket.Status != AssignmentConst.STATUS_ACTIVE)
            {
                releaseReason = "bị trả khi phiếu đã DONE";
            }
            else if (!await _assignmentRepository.IsMemberAsync(submission.ProjectId, submission.DocumentTypeId, dataHolder.UserId))
            {
                releaseReason = "bị trả khi người nhập đã bị gỡ khỏi dự án";
                ticket.DecreaseDocuments(1, modifiedBy, now);
            }
            else if (dataHolder.IncreaseReturnAndShouldRelease(modifiedBy, now))
            {
                releaseReason = $"bị trả lần {dataHolder.ReturnCount}";
                ticket.DecreaseDocuments(1, modifiedBy, now);
            }
            if (releaseReason != null)
            {
                _assignmentRepository.DeleteHolder(dataHolder);
            }
            return new QaResultOnEntry(ticket, dataHolder.UserId, releaseReason);
        }

        public async Task<bool> TryMarkDoneAsync(AssignmentTicket ticket, bool isQaEnabled, DateTime doneAt)
        {
            if (ticket.Status == AssignmentConst.STATUS_ACTIVE && await _assignmentRepository.CountDataNotDoneAsync(ticket.Id, isQaEnabled) == 0)
            {
                return await _assignmentRepository.TryMarkDoneAsync(ticket.Id, doneAt) > 0;
            }
            return false;
        }

        public async Task<AssignmentChangePlan> PlanRevokeAsync(OperationAccess access, int? ticketId, string? holderUserId, List<AssignmentItem>? items)
        {
            var isByTicket = ticketId != null && holderUserId == null && (items == null || items.Count == 0);
            var isByNode = ticketId == null && holderUserId != null && items is { Count: > 0 };
            if (!isByTicket && !isByNode)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Gửi ticketId, hoặc assigneeUserId kèm hộp / hồ sơ");
            }
            var projectId = access.Project.Id;
            var documentTypeId = access.DocumentType.Id;

            // Theo phiếu: phạm vi = các nút của phiếu; theo nút: mọi phiếu Data ACTIVE của người đó trong cặp
            List<AssignmentTicket> tickets;
            List<AssignmentItem> scopeItems;
            if (isByTicket)
            {
                var ticket = await _assignmentRepository.FindTicketByIdAsync(ticketId!.Value);
                if (ticket == null || ticket.ProjectId != projectId || ticket.DocumentTypeId != documentTypeId || ticket.Role != RoleConstants.ROLE_DATA)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
                }
                ticket.EnsureActive();
                holderUserId = ticket.AssigneeUserId;
                tickets = [ticket];
                scopeItems = (await _assignmentRepository.GetTicketItemsAsync(ticket.Id))
                    .Select(item => new AssignmentItem(item.Box, item.ProfileRange ?? ""))
                    .ToList();
            }
            else
            {
                tickets = await _projectMemberRepository.GetActiveAssignmentTicketsAsync(projectId, documentTypeId, holderUserId!, RoleConstants.ROLE_DATA);
                if (tickets.Count == 0)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotActive);
                }
                scopeItems = NormalizeItems(items!);
            }

            await EnsureManagedHolderAsync(access, holderUserId!, RoleConstants.ROLE_DATA);

            var ticketIds = tickets.Select(ticket => ticket.Id).ToHashSet();
            var rows = (await _assignmentRepository.GetHolderRowsAsync(projectId, documentTypeId, RoleConstants.ROLE_DATA, holderUserId!, scopeItems))
                .Where(row => row.StatusCheck == SubmissionConst.NOT_CHECKED && ticketIds.Contains(row.TicketId))
                .ToList();
            if (rows.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorNothingToRevoke);
            }
            var affectedTicketIds = rows.Select(row => row.TicketId).ToHashSet();
            return new AssignmentChangePlan(holderUserId!, tickets.Where(ticket => affectedTicketIds.Contains(ticket.Id)).ToList(), rows);
        }

        public async Task<AssignmentChangePlan> PlanTransferAsync(OperationAccess access, string fromUserId, string toUserId, int? ticketId, List<AssignmentItem>? items)
        {
            if (fromUserId == toUserId)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Người nhận phải khác người chuyển");
            }
            // Phía người chuyển dùng chung luật thu hồi; chuyển theo phiếu thì phiếu phải của đúng người chuyển
            var plan = await PlanRevokeAsync(access, ticketId, ticketId == null ? fromUserId : null, items);
            if (plan.HolderUserId != fromUserId)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
            }
            await EnsureAssigneeAsync(access, toUserId, RoleConstants.ROLE_DATA);
            if (plan.Rows.Count > AssignmentConst.DATA_TICKET_MAX_DOCUMENTS)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorTicketTooLarge);
            }
            return plan;
        }

        public async Task<QaOwnershipChangePlan> PlanQaRevokeAsync(OperationAccess access, int ticketId, List<AssignmentItem>? items)
        {
            access.Project.EnsureQaEnabled();
            var projectId = access.Project.Id;
            var documentTypeId = access.DocumentType.Id;
            var ownership = await _assignmentRepository.FindTicketByIdAsync(ticketId);
            if (ownership == null || ownership.ProjectId != projectId || ownership.DocumentTypeId != documentTypeId || ownership.Role != RoleConstants.ROLE_QA)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorTicketNotFound);
            }
            ownership.EnsureActive();
            await EnsureManagedHolderAsync(access, ownership.AssigneeUserId, RoleConstants.ROLE_QA);

            // Không gửi nút = lấy cả phụ trách; gửi nút thì mỗi hộp / hồ sơ phải nằm trong phụ trách (hộp = mọi hồ sơ phụ trách của hộp đó)
            var ownedItems = await _assignmentRepository.GetTicketItemsAsync(ownership.Id);
            var removedItems = ownedItems;
            if (items is { Count: > 0 })
            {
                List<AssignmentItem> foreignItems = [];
                HashSet<int> removedItemIds = [];
                foreach (var item in NormalizeItems(items))
                {
                    var matchedItems = ownedItems
                        .Where(owned => string.Equals(owned.Box, item.Box, StringComparison.OrdinalIgnoreCase)
                            && (item.ProfileRange == null || string.Equals(owned.ProfileRange, item.ProfileRange, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    if (matchedItems.Count == 0)
                    {
                        foreignItems.Add(item);
                    }
                    removedItemIds.UnionWith(matchedItems.Select(owned => owned.Id));
                }
                if (foreignItems.Count > 0)
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest,
                        $"Hộp / hồ sơ không thuộc phụ trách: {string.Join(", ", foreignItems.Select(item => item.ProfileRange == null ? item.Box : $"{item.Box} / {item.ProfileRange}"))}");
                }
                removedItems = ownedItems.Where(owned => removedItemIds.Contains(owned.Id)).ToList();
            }

            // K4 giảm theo mọi văn bản QA đó giữ trong nút bị lấy (cả văn bản đã đạt); chỉ văn bản chưa đạt bị lấy khỏi QA đó (14.4)
            var rows = await _assignmentRepository.GetHolderRowsAsync(projectId, documentTypeId, RoleConstants.ROLE_QA, ownership.AssigneeUserId,
                removedItems.Select(item => new AssignmentItem(item.Box, item.ProfileRange ?? "")).ToList());
            var unfinishedRows = rows.Where(row => row.StatusQa != SubmissionConst.QA_PASSED).ToList();
            if (removedItems.Count == 0 && unfinishedRows.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorNothingToRevoke);
            }
            return new QaOwnershipChangePlan(ownership, removedItems, removedItems.Count == ownedItems.Count, rows.Count, unfinishedRows);
        }

        public async Task<QaOwnershipChangePlan> PlanQaTransferAsync(OperationAccess access, int ticketId, string toUserId, List<AssignmentItem>? items)
        {
            var plan = await PlanQaRevokeAsync(access, ticketId, items);
            var projectId = access.Project.Id;
            var documentTypeId = access.DocumentType.Id;
            if (plan.Ownership.AssigneeUserId == toUserId)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Người nhận phải khác người chuyển");
            }
            await EnsureAssigneeAsync(access, toUserId, RoleConstants.ROLE_QA);
            if (await _assignmentRepository.FindQaReviewerModeAsync(projectId, toUserId) == QaAssignmentConst.MODE_SAMPLE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Chưa hỗ trợ giao phiếu chế độ SAMPLE");
            }

            // Người nhận không được chấm văn bản mình từng kiểm hoặc đang giữ nhập liệu
            var unfinishedIds = plan.UnfinishedRows.Select(row => row.SubmissionId).ToHashSet();
            var removedNodes = plan.RemovedItems.Select(item => new AssignmentItem(item.Box, item.ProfileRange ?? "")).ToList();
            if ((await _assignmentRepository.GetSubmissionIdsCheckedByAsync([.. unfinishedIds], toUserId)).Count > 0
                || (await _assignmentRepository.GetHolderRowsAsync(projectId, documentTypeId, RoleConstants.ROLE_DATA, toUserId, removedNodes))
                    .Any(row => unfinishedIds.Contains(row.SubmissionId)))
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaSameAsEntry);
            }

            // Nút đang chuyển chỉ được thuộc phụ trách nguồn; phụ trách sẵn có của người nhận ở nút khác thì gộp vào
            var removedNodeKeys = plan.RemovedItems.Select(item => ToNodeKey(item.Box, item.ProfileRange ?? "")).ToHashSet();
            var ownerships = await _assignmentRepository.GetActiveQaOwnershipsAsync(projectId, documentTypeId);
            if (ownerships.Any(owner => owner.TicketId != plan.Ownership.Id && removedNodeKeys.Contains(ToNodeKey(owner.Box, owner.ProfileRange))))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorAssignmentConflict);
            }
            if (plan.UnfinishedRows.Count > QaAssignmentConst.TICKET_MAX_DOCUMENTS)
            {
                throw new UserFriendlyException(ErrorCodes.ConstructionErrorQaTicketTooLarge);
            }
            return plan with { TargetOwnershipTicketId = ownerships.FirstOrDefault(owner => owner.QaUserId == toUserId)?.TicketId };
        }

        // Nhả các nút khỏi phụ trách nguồn và xoá văn bản chưa đạt của QA đó; trả số văn bản xoá thật
        public async Task<int> ReleaseQaOwnershipAsync(QaOwnershipChangePlan plan, string? modifiedBy, DateTime now)
        {
            if (plan.IsAllNodesRemoved)
            {
                plan.Ownership.Cancel(modifiedBy, now);
            }
            else
            {
                // K4: phụ trách còn nút thì giảm số bằng UPDATE, không qua DecreaseDocuments (về 0 sẽ tự CANCELLED)
                _assignmentRepository.RemoveTicketItems(plan.RemovedItems);
                await _assignmentRepository.DecreaseOwnershipDocumentsAsync(plan.Ownership.Id, plan.RemovedDocuments, modifiedBy, now);
            }

            // K3: đợt ACTIVE về 0 → Cancel; còn văn bản và đều đã đạt → Pass; đợt PASSED / REJECTED chỉ giảm số
            var rowsByBatch = plan.UnfinishedRows.GroupBy(row => row.TicketId).ToDictionary(group => group.Key, group => group.ToList());
            var batches = await _assignmentRepository.FindQaBatchesAsync([.. rowsByBatch.Keys]);
            var releasedCount = 0;
            foreach (var batch in batches)
            {
                var deletedCount = await _assignmentRepository.DeleteUnfinishedHoldersAsync(RoleConstants.ROLE_QA, plan.Ownership.AssigneeUserId,
                    rowsByBatch[batch.Id].Select(row => row.SubmissionId).ToList());
                if (deletedCount == 0)
                {
                    continue;
                }
                releasedCount += deletedCount;
                await _assignmentRepository.DecreaseQaBatchDocumentsAsync(batch.Id, deletedCount, modifiedBy, now);
                if (batch.Status != QaAssignmentConst.TICKET_STATUS_ACTIVE)
                {
                    continue;
                }
                // batch đọc trước lệnh UPDATE nên DocumentCount còn là số cũ
                if (batch.DocumentCount <= deletedCount)
                {
                    batch.Cancel(modifiedBy, now);
                }
                else if (await _assignmentRepository.CountNotPassedInQaBatchAsync(batch.Id) == 0)
                {
                    batch.Pass(modifiedBy, now);
                }
            }
            return releasedCount;
        }

        // Leader chỉ đụng người mình quản lý đúng vai trò của cặp; tính cả thành viên đang tắt (14.8)
        private async Task EnsureManagedHolderAsync(OperationAccess access, string holderUserId, string role)
        {
            if (!access.IsAdmin && !(await _projectMemberRepository.GetManagedMembersAsync(access.Project.Id, access.DocumentType.Id, access.LeaderUserId!))
                .Any(member => member.UserId == holderUserId && member.Role == role))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderForbiddenTarget);
            }
        }

        // Người nhận phải là thành viên đang hoạt động đúng vai trò của cặp (811); Leader chỉ giao cho người mình quản lý (821)
        private async Task EnsureAssigneeAsync(OperationAccess access, string assigneeUserId, string role)
        {
            var member = await _assignmentRepository.FindActiveMemberAsync(access.Project.Id, access.DocumentType.Id, assigneeUserId, role)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorAssigneeNotMember);
            if (!access.IsAdmin && !member.IsManagedBy(access.LeaderUserId!))
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderForbiddenTarget);
            }
        }
    }

    public record AssignmentItem(string Box, string? ProfileRange);

    public record AssignmentNode(string Box, string ProfileRange);

    public record AssignmentSubmissionRow(int SubmissionId, string Box, string ProfileRange, int StatusCheck, int StatusQa,
        string? DataHolderUserId, string? QaHolderUserId);

    public record AssignmentHolderRow(int SubmissionId, int TicketId, string Box, string ProfileRange, int StatusCheck, int StatusQa);

    public record QaOwnership(int TicketId, string QaUserId, string Box, string ProfileRange);

    public record QaResultOnEntry(AssignmentTicket Ticket, string HolderUserId, string? ReleaseReason);

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

    public record AssignmentChangePlan(string HolderUserId, List<AssignmentTicket> Tickets, List<AssignmentHolderRow> Rows);

    public record QaOwnershipChangePlan(AssignmentTicket Ownership, List<AssignmentTicketItem> RemovedItems, bool IsAllNodesRemoved, int RemovedDocuments,
        List<AssignmentHolderRow> UnfinishedRows, int? TargetOwnershipTicketId = null);
}
