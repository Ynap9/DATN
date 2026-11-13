using AutoMapper;
using Microsoft.EntityFrameworkCore;
using sh.be.applications.Module.Operation.Dtos.Assignment;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.domain.Module.Operation.Assignments.Services;
using sh.be.infrastructure.data.Base;
using sh.be.infrastructure.data.Persistence.Db;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Construction;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.infrastructure.data.Module.Operation.Queries
{
    public class AssignmentQuery : BaseQuery, IAssignmentQuery
    {
        private const string BinaryCollation = "Latin1_General_BIN2";

        public AssignmentQuery(ShDbContext shDbContext, IMapper mapper) : base(shDbContext, mapper)
        {
        }

        public Task<BaseResponsePagingDto<AssignmentNodeDto>> FindBoxNodes(int projectId, int documentTypeId, FindAssignmentTreeDto dto)
        {
            return FindNodesAsync(projectId, documentTypeId, dto, null);
        }

        public Task<BaseResponsePagingDto<AssignmentNodeDto>> FindProfileNodes(int projectId, int documentTypeId, FindAssignmentProfilesDto dto)
        {
            return FindNodesAsync(projectId, documentTypeId, dto, dto.Box?.Trim() ?? String.Empty);
        }

        public async Task<ViewAssignmentTicketDto?> FindTicketById(int ticketId)
        {
            var ticket = await _shDbContext.AssignmentTickets
                .AsNoTracking()
                .Where(t => t.Id == ticketId)
                .Select(t => new ViewAssignmentTicketDto
                {
                    Id = t.Id,
                    Role = t.Role,
                    ProjectId = t.ProjectId,
                    ProjectName = _shDbContext.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault(),
                    DocumentTypeId = t.DocumentTypeId,
                    DocumentTypeName = _shDbContext.DocumentTypes.Where(d => d.Id == t.DocumentTypeId).Select(d => d.Name).FirstOrDefault(),
                    AssigneeUserId = t.AssigneeUserId,
                    AssigneeFullName = _shDbContext.Users.Where(u => u.Id == t.AssigneeUserId).Select(u => u.FullName).FirstOrDefault(),
                    Status = t.Status,
                    DocumentCount = t.DocumentCount,
                    CreatedBy = t.CreatedBy,
                    CreatedByFullName = _shDbContext.Users.Where(u => u.Id == t.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
                    CreatedDate = t.CreatedDate,
                    DoneAt = t.DoneAt,
                })
                .FirstOrDefaultAsync();
            if (ticket == null)
            {
                return null;
            }

            ticket.Items = await _shDbContext.AssignmentTicketItems
                .AsNoTracking()
                .Where(i => i.AssignmentTicketId == ticketId)
                .OrderBy(i => i.Id)
                .Select(i => new AssignmentItemDto { Box = i.Box, ProfileRange = i.ProfileRange })
                .ToListAsync();

            var heldSubmissions = await (from u in _shDbContext.SubmissionUsers.AsNoTracking()
                                         where u.Role == ticket.Role
                                             && (ticket.Role == RoleConstants.ROLE_DATA
                                                 ? u.TicketId == ticketId
                                                 : u.ProjectId == ticket.ProjectId && u.DocumentTypeId == ticket.DocumentTypeId && u.UserId == ticket.AssigneeUserId)
                                         join s in _shDbContext.Submissions on u.SubmissionId equals s.Id
                                         select new { s.Box, s.ProfileRange, s.StatusCheck, s.StatusQa })
                                        .ToListAsync();
            var itemKeys = ticket.Items.Select(item => AssignmentDomainService.ToNodeKey(item.Box, item.ProfileRange ?? "")).ToHashSet();
            var ticketSubmissions = ticket.Role == RoleConstants.ROLE_DATA
                ? heldSubmissions
                : heldSubmissions.Where(s => itemKeys.Contains(AssignmentDomainService.ToNodeKey(s.Box, s.ProfileRange))).ToList();
            ticket.Total = ticketSubmissions.Count;
            ticket.Checked = ticketSubmissions.Count(s => s.StatusCheck == SubmissionConst.CHECKED);
            ticket.Returned = ticketSubmissions.Count(s => s.StatusCheck == SubmissionConst.RETURNED);
            ticket.QaPassed = ticketSubmissions.Count(s => s.StatusQa == SubmissionConst.QA_PASSED);
            return ticket;
        }

        // Danh sách phiếu nhập liệu / phụ trách QA của cặp, mới nhất trước, không kèm nút; Leader chỉ thấy phiếu người mình quản lý
        public async Task<BaseResponsePagingDto<ViewAssignmentTicketDto>> FindTicketPaging(int projectId, int documentTypeId, FindPagingAssignmentTicketDto dto,
            string? managedByLeaderUserId)
        {
            var query = _shDbContext.AssignmentTickets
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId && t.DocumentTypeId == documentTypeId);
            if (managedByLeaderUserId != null)
            {
                query = query.Where(t => _shDbContext.ProjectMembers.Any(m => m.ProjectId == t.ProjectId && m.DocumentTypeId == t.DocumentTypeId
                    && m.UserId == t.AssigneeUserId && m.Role == t.Role && m.ManagedByUserId == managedByLeaderUserId));
            }
            if (!string.IsNullOrEmpty(dto.Role))
            {
                query = query.Where(t => t.Role == dto.Role);
            }
            if (!string.IsNullOrEmpty(dto.AssigneeUserId))
            {
                query = query.Where(t => t.AssigneeUserId == dto.AssigneeUserId);
            }
            if (dto.Status.HasValue)
            {
                query = query.Where(t => t.Status == dto.Status);
            }

            var totalItems = await query.CountAsync();
            var tickets = await query
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.Id)
                .Paging(dto)
                .Select(t => new ViewAssignmentTicketDto
                {
                    Id = t.Id,
                    Role = t.Role,
                    ProjectId = t.ProjectId,
                    ProjectName = _shDbContext.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault(),
                    DocumentTypeId = t.DocumentTypeId,
                    DocumentTypeName = _shDbContext.DocumentTypes.Where(d => d.Id == t.DocumentTypeId).Select(d => d.Name).FirstOrDefault(),
                    AssigneeUserId = t.AssigneeUserId,
                    AssigneeFullName = _shDbContext.Users.Where(u => u.Id == t.AssigneeUserId).Select(u => u.FullName).FirstOrDefault(),
                    Status = t.Status,
                    DocumentCount = t.DocumentCount,
                    CreatedBy = t.CreatedBy,
                    CreatedByFullName = _shDbContext.Users.Where(u => u.Id == t.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
                    CreatedDate = t.CreatedDate,
                    DoneAt = t.DoneAt,
                })
                .ToListAsync();

            // Đếm như FindTicketById: phiếu Data theo TicketId, phụ trách QA theo văn bản QA đó giữ trong các nút phụ trách
            var ticketIds = tickets.Select(t => t.Id).ToList();
            var statsByTicket = await (from t in _shDbContext.AssignmentTickets.AsNoTracking()
                                       where ticketIds.Contains(t.Id)
                                       from u in _shDbContext.SubmissionUsers
                                       where u.Role == t.Role
                                           && (t.Role == RoleConstants.ROLE_DATA
                                               ? u.TicketId == t.Id
                                               : u.ProjectId == t.ProjectId && u.DocumentTypeId == t.DocumentTypeId && u.UserId == t.AssigneeUserId)
                                       join s in _shDbContext.Submissions on u.SubmissionId equals s.Id
                                       where t.Role == RoleConstants.ROLE_DATA
                                           || _shDbContext.AssignmentTicketItems.Any(i => i.AssignmentTicketId == t.Id && i.Box == s.Box && i.ProfileRange == s.ProfileRange)
                                       group s by t.Id into g
                                       select new
                                       {
                                           TicketId = g.Key,
                                           Total = g.Count(),
                                           Checked = g.Count(s => s.StatusCheck == SubmissionConst.CHECKED),
                                           Returned = g.Count(s => s.StatusCheck == SubmissionConst.RETURNED),
                                           QaPassed = g.Count(s => s.StatusQa == SubmissionConst.QA_PASSED),
                                       })
                                      .ToDictionaryAsync(stats => stats.TicketId);
            foreach (var ticket in tickets)
            {
                if (statsByTicket.TryGetValue(ticket.Id, out var stats))
                {
                    ticket.Total = stats.Total;
                    ticket.Checked = stats.Checked;
                    ticket.Returned = stats.Returned;
                    ticket.QaPassed = stats.QaPassed;
                }
            }

            return new BaseResponsePagingDto<ViewAssignmentTicketDto>
            {
                Items = tickets,
                TotalItems = totalItems,
            };
        }

        public async Task<BaseResponsePagingDto<GetListMyAssignmentDto>> FindMyAssignments(string userId, FindMyAssignmentDto dto)
        {
            List<GetListMyAssignmentDto> tickets = [];
            if (dto.Kind != RoleConstants.ROLE_QA)
            {
                var dataTickets = _shDbContext.AssignmentTickets
                    .AsNoTracking()
                    .Where(t => t.AssigneeUserId == userId && t.Role == RoleConstants.ROLE_DATA
                        && (t.Status == AssignmentConst.STATUS_ACTIVE || (dto.IncludeFinished && t.Status == AssignmentConst.STATUS_DONE))
                        && _shDbContext.Projects.Any(p => p.Id == t.ProjectId));
                if (dto.ProjectId.HasValue)
                {
                    dataTickets = dataTickets.Where(t => t.ProjectId == dto.ProjectId);
                }
                if (dto.DocumentTypeId.HasValue)
                {
                    dataTickets = dataTickets.Where(t => t.DocumentTypeId == dto.DocumentTypeId);
                }
                tickets.AddRange(await dataTickets
                    .Select(t => new GetListMyAssignmentDto
                    {
                        Kind = RoleConstants.ROLE_DATA,
                        TicketId = t.Id,
                        ProjectId = t.ProjectId,
                        ProjectName = _shDbContext.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault(),
                        DocumentTypeId = t.DocumentTypeId,
                        DocumentTypeName = _shDbContext.DocumentTypes.Where(d => d.Id == t.DocumentTypeId).Select(d => d.Name).FirstOrDefault(),
                        Status = t.Status.ToString(),
                        DocumentCount = t.DocumentCount,
                        Total = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_DATA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId)),
                        Checked = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_DATA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusCheck == SubmissionConst.CHECKED)),
                        Returned = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_DATA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusCheck == SubmissionConst.RETURNED)),
                        QaPassed = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_DATA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa == SubmissionConst.QA_PASSED)),
                        CreatedDate = t.CreatedDate,
                    })
                    .ToListAsync());
            }
            if (dto.Kind != RoleConstants.ROLE_DATA)
            {
                var qaBatches = _shDbContext.QaTickets
                    .AsNoTracking()
                    .Where(t => t.QaUserId == userId
                        && (t.Status == QaAssignmentConst.TICKET_STATUS_ACTIVE || t.Status == QaAssignmentConst.TICKET_STATUS_REJECTED
                            || (dto.IncludeFinished && t.Status == QaAssignmentConst.TICKET_STATUS_PASSED))
                        && _shDbContext.Projects.Any(p => p.Id == t.ProjectId));
                if (dto.ProjectId.HasValue)
                {
                    qaBatches = qaBatches.Where(t => t.ProjectId == dto.ProjectId);
                }
                if (dto.DocumentTypeId.HasValue)
                {
                    qaBatches = qaBatches.Where(t => t.DocumentTypeId == dto.DocumentTypeId);
                }
                tickets.AddRange(await qaBatches
                    .Select(t => new GetListMyAssignmentDto
                    {
                        Kind = RoleConstants.ROLE_QA,
                        TicketId = t.Id,
                        ProjectId = t.ProjectId,
                        ProjectName = _shDbContext.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault(),
                        DocumentTypeId = t.DocumentTypeId,
                        DocumentTypeName = _shDbContext.DocumentTypes.Where(d => d.Id == t.DocumentTypeId).Select(d => d.Name).FirstOrDefault(),
                        Status = t.Status,
                        DocumentCount = t.DocumentCount,
                        Total = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId)),
                        Checked = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusCheck == SubmissionConst.CHECKED)),
                        Returned = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusCheck == SubmissionConst.RETURNED)),
                        QaPassed = _shDbContext.SubmissionUsers.Count(u => u.TicketId == t.Id && u.Role == RoleConstants.ROLE_QA
                            && _shDbContext.Submissions.Any(s => s.Id == u.SubmissionId && s.StatusQa == SubmissionConst.QA_PASSED)),
                        CreatedDate = t.CreatedDate,
                    })
                    .ToListAsync());
            }

            var pageTickets = tickets
                .OrderByDescending(t => t.CreatedDate)
                .ThenByDescending(t => t.TicketId)
                .Skip(dto.GetSkip())
                .Take(dto.PageSize)
                .ToList();
            var dataTicketIds = pageTickets.Where(t => t.Kind == RoleConstants.ROLE_DATA).Select(t => t.TicketId).ToList();
            var itemsByTicket = (await _shDbContext.AssignmentTicketItems
                    .AsNoTracking()
                    .Where(i => dataTicketIds.Contains(i.AssignmentTicketId))
                    .OrderBy(i => i.Id)
                    .ToListAsync())
                .ToLookup(i => i.AssignmentTicketId, i => new AssignmentItemDto { Box = i.Box, ProfileRange = i.ProfileRange });
            foreach (var ticket in pageTickets.Where(t => t.Kind == RoleConstants.ROLE_DATA))
            {
                ticket.Items = itemsByTicket[ticket.TicketId].ToList();
            }

            return new BaseResponsePagingDto<GetListMyAssignmentDto>
            {
                Items = pageTickets,
                TotalItems = tickets.Count,
            };
        }

        public Task<Dictionary<string, string>> FindFullNames(List<string> userIds)
        {
            return _shDbContext.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
        }

        private async Task<BaseResponsePagingDto<AssignmentNodeDto>> FindNodesAsync(int projectId, int documentTypeId, FindAssignmentTreeDto dto, string? box)
        {
            var isQa = dto.Role == RoleConstants.ROLE_QA;
            var holderRole = isQa ? RoleConstants.ROLE_QA : RoleConstants.ROLE_DATA;
            var holderUserId = dto.HolderUserId ?? String.Empty;

            var rows = from s in _shDbContext.Submissions.AsNoTracking()
                       where s.ProjectId == projectId && s.DocumentTypeId == documentTypeId
                       join dataHolder in _shDbContext.SubmissionUsers.Where(u => u.Role == RoleConstants.ROLE_DATA) on s.Id equals dataHolder.SubmissionId into dataHolders
                       from dataHolder in dataHolders.DefaultIfEmpty()
                       join qaHolder in _shDbContext.SubmissionUsers.Where(u => u.Role == RoleConstants.ROLE_QA) on s.Id equals qaHolder.SubmissionId into qaHolders
                       from qaHolder in qaHolders.DefaultIfEmpty()
                       select new
                       {
                           s.Box,
                           s.ProfileRange,
                           s.StatusCheck,
                           s.StatusQa,
                           DataUserId = dataHolder == null ? null : dataHolder.UserId,
                           QaUserId = qaHolder == null ? null : qaHolder.UserId,
                       };
            if (box != null)
            {
                rows = rows.Where(r => r.Box == box);
            }
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                rows = rows.Where(r => r.Box.Contains(dto.Keyword) || r.ProfileRange.Contains(dto.Keyword));
            }

            var groupedRows = box == null ? rows.GroupBy(r => r.Box) : rows.GroupBy(r => r.ProfileRange);
            var nodeStats = await groupedRows
                .Select(g => new
                {
                    Key = g.Min(r => EF.Functions.Collate(box == null ? r.Box : r.ProfileRange, BinaryCollation)),
                    Count = g.Count(),
                    DataAssigned = g.Sum(r => r.DataUserId != null ? 1 : 0),
                    CheckedCount = g.Sum(r => r.StatusCheck == SubmissionConst.CHECKED ? 1 : 0),
                    ReturnedCount = g.Sum(r => r.StatusCheck == SubmissionConst.RETURNED ? 1 : 0),
                    NotCheckedCount = g.Sum(r => r.StatusCheck == SubmissionConst.NOT_CHECKED ? 1 : 0),
                    QaPassed = g.Sum(r => r.StatusQa == SubmissionConst.QA_PASSED ? 1 : 0),
                    QaAssigned = g.Sum(r => r.StatusCheck == SubmissionConst.CHECKED && r.QaUserId != null ? 1 : 0),
                    QaUnassigned = g.Sum(r => r.StatusCheck == SubmissionConst.CHECKED && r.StatusQa == SubmissionConst.QA_NOT_REVIEWED && r.QaUserId == null ? 1 : 0),
                    DataHeldByHolder = g.Sum(r => r.DataUserId == holderUserId ? 1 : 0),
                    QaHeldByHolder = g.Sum(r => r.QaUserId == holderUserId ? 1 : 0),
                })
                .ToListAsync();

            var nodes = nodeStats
                .Where(g => !isQa || g.CheckedCount > 0)
                .Where(g => dto.HolderUserId == null || (isQa ? g.QaHeldByHolder : g.DataHeldByHolder) > 0)
                .Select(g => new AssignmentNodeDto
                {
                    Box = box ?? g.Key,
                    ProfileRange = box == null ? null : g.Key,
                    Total = isQa ? g.CheckedCount : g.Count,
                    Assigned = isQa ? g.QaAssigned : g.DataAssigned,
                    Unassigned = isQa ? g.QaUnassigned : g.Count - g.DataAssigned,
                    Checked = g.CheckedCount,
                    Returned = g.ReturnedCount,
                    QaPassed = g.QaPassed,
                    NotChecked = isQa ? g.Count - g.CheckedCount : g.NotCheckedCount,
                })
                .Where(node => !dto.OnlyUnassigned || node.Unassigned > 0)
                .OrderBy(node => box == null ? node.Box : node.ProfileRange, Comparer<string?>.Create(AssignmentDomainService.CompareNatural))
                .ToList();
            var pageNodes = nodes.Skip(dto.GetSkip()).Take(dto.PageSize).ToList();
            if (pageNodes.Count == 0)
            {
                return new BaseResponsePagingDto<AssignmentNodeDto> { Items = pageNodes, TotalItems = nodes.Count };
            }

            string ToPageNodeKey(string rowBox, string rowProfileRange) => (box == null ? rowBox : rowProfileRange).Trim().ToUpperInvariant();
            List<string> pageBoxes = box == null ? pageNodes.Select(node => node.Box).ToList() : [box];

            var holderCounts = await (from u in _shDbContext.SubmissionUsers.AsNoTracking()
                                      where u.ProjectId == projectId && u.DocumentTypeId == documentTypeId && u.Role == holderRole
                                      join s in _shDbContext.Submissions on u.SubmissionId equals s.Id
                                      where pageBoxes.Contains(s.Box)
                                      group u by new { s.Box, s.ProfileRange, u.UserId } into g
                                      select new { g.Key.Box, g.Key.ProfileRange, g.Key.UserId, DocumentCount = g.Count() })
                                     .ToListAsync();
            var holdersByNode = holderCounts
                .GroupBy(h => ToPageNodeKey(h.Box, h.ProfileRange))
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(h => h.UserId)
                        .Select(userGroup => new AssignmentHolderDto { UserId = userGroup.Key, DocumentCount = userGroup.Sum(h => h.DocumentCount) })
                        .OrderByDescending(holder => holder.DocumentCount)
                        .ThenBy(holder => holder.UserId)
                        .ToList());

            Dictionary<string, string?> ownerByNode = [];
            if (isQa)
            {
                var ownerships = await (from t in _shDbContext.AssignmentTickets.AsNoTracking()
                                        where t.ProjectId == projectId && t.DocumentTypeId == documentTypeId && t.Role == RoleConstants.ROLE_QA
                                            && t.Status == AssignmentConst.STATUS_ACTIVE
                                        join i in _shDbContext.AssignmentTicketItems on t.Id equals i.AssignmentTicketId
                                        where pageBoxes.Contains(i.Box)
                                        select new { i.Box, ProfileRange = i.ProfileRange ?? "", t.AssigneeUserId })
                                       .ToListAsync();
                var ownerByProfile = ownerships
                    .GroupBy(o => AssignmentDomainService.ToNodeKey(o.Box, o.ProfileRange))
                    .ToDictionary(g => g.Key, g => g.First().AssigneeUserId);
                if (box == null)
                {
                    var boxProfiles = await _shDbContext.Submissions
                        .AsNoTracking()
                        .Where(s => s.ProjectId == projectId && s.DocumentTypeId == documentTypeId && pageBoxes.Contains(s.Box))
                        .Select(s => new { s.Box, s.ProfileRange })
                        .Distinct()
                        .ToListAsync();
                    ownerByNode = boxProfiles
                        .GroupBy(p => ToPageNodeKey(p.Box, p.ProfileRange))
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var profileOwners = g.Select(p => ownerByProfile.GetValueOrDefault(AssignmentDomainService.ToNodeKey(p.Box, p.ProfileRange))).Distinct().ToList();
                                return profileOwners.Count == 1 ? profileOwners[0] : null;
                            });
                }
                else
                {
                    ownerByNode = pageNodes.ToDictionary(
                        node => ToPageNodeKey(box, node.ProfileRange!),
                        node => (string?)ownerByProfile.GetValueOrDefault(AssignmentDomainService.ToNodeKey(box, node.ProfileRange!)));
                }
            }

            var userIds = holdersByNode.Values
                .SelectMany(holders => holders.Take(AssignmentConst.NODE_HOLDERS_MAX).Select(holder => holder.UserId))
                .Concat(ownerByNode.Values.OfType<string>())
                .Distinct()
                .ToList();
            var fullNames = await FindFullNames(userIds);
            foreach (var node in pageNodes)
            {
                var nodeKey = ToPageNodeKey(node.Box, node.ProfileRange ?? "");
                var holders = holdersByNode.GetValueOrDefault(nodeKey, []);
                node.HolderCount = holders.Count;
                node.Holders = holders.Take(AssignmentConst.NODE_HOLDERS_MAX).ToList();
                node.Holders.ForEach(holder => holder.FullName = fullNames.GetValueOrDefault(holder.UserId));
                node.OwnerUserId = ownerByNode.GetValueOrDefault(nodeKey);
                node.OwnerFullName = node.OwnerUserId == null ? null : fullNames.GetValueOrDefault(node.OwnerUserId);
            }

            return new BaseResponsePagingDto<AssignmentNodeDto>
            {
                Items = pageNodes,
                TotalItems = nodes.Count,
            };
        }
    }
}
