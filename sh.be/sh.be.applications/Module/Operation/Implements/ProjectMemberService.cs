using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Operation.Dtos.ProjectMember;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Entities;
using sh.be.domain.Module.Operation.ProjectMembers.Models;
using sh.be.domain.Module.Operation.ProjectMembers.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.domain.Module.Operation.StaffGroups.Repositories;
using sh.be.domain.Module.Operation.StaffGroups.Services;
using sh.be.shared.Constants.Auth;
using sh.be.shared.Constants.Core;
using sh.be.shared.Constants.Operation;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using System.Text.Json;

namespace sh.be.applications.Module.Operation.Implements
{
    public class ProjectMemberService : BaseService, IProjectMemberService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IStaffGroupRepository _staffGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProjectMemberQuery _projectMemberQuery;
        private readonly OperationAccessDomainService _operationAccessDomainService;
        private readonly ProjectMemberDomainService _projectMemberDomainService;
        private readonly StaffGroupDomainService _staffGroupDomainService;

        public ProjectMemberService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<ProjectMemberService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IProjectMemberRepository projectMemberRepository, IStaffGroupRepository staffGroupRepository, IUserRepository userRepository, IProjectMemberQuery projectMemberQuery,
            OperationAccessDomainService operationAccessDomainService, ProjectMemberDomainService projectMemberDomainService, StaffGroupDomainService staffGroupDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _projectMemberRepository = projectMemberRepository;
            _staffGroupRepository = staffGroupRepository;
            _userRepository = userRepository;
            _projectMemberQuery = projectMemberQuery;
            _operationAccessDomainService = operationAccessDomainService;
            _projectMemberDomainService = projectMemberDomainService;
            _staffGroupDomainService = staffGroupDomainService;
        }

        public async Task<BaseResponsePagingDto<ViewProjectMemberDto>> FindPaging(int projectId, FindPagingProjectMemberDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > ProjectMemberConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {ProjectMemberConst.MAX_PAGE_SIZE}");
            }
            if (dto.Role != null && !ProjectMemberConst.ROLES.Contains(dto.Role))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {string.Join(", ", ProjectMemberConst.ROLES)}");
            }
            var access = await _operationAccessDomainService.EnsureCanManageAsync(projectId, dto.DocumentTypeId, getCurrentUserId(), false);
            return await _projectMemberQuery.FindPaging(projectId, dto, access.LeaderUserId);
        }

        public async Task<BaseResponsePagingDto<ProjectMemberCandidateDto>> FindCandidates(int projectId, FindProjectMemberCandidateDto dto)
        {
            _logger.LogInformation($"{nameof(FindCandidates)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > ProjectMemberConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {ProjectMemberConst.MAX_PAGE_SIZE}");
            }
            if (!ProjectMemberConst.ROLES.Contains(dto.Role))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {string.Join(", ", ProjectMemberConst.ROLES)}");
            }
            var currentUserId = getCurrentUserId();
            var access = await _operationAccessDomainService.EnsureCanManageAsync(projectId, dto.DocumentTypeId, currentUserId, false);
            if (!access.IsAdmin && dto.Role == RoleConstants.ROLE_LEADER)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorNotProjectLeader);
            }
            if (dto.StaffGroupId.HasValue)
            {
                await _staffGroupDomainService.EnsureAccessibleAsync(dto.StaffGroupId.Value, currentUserId, access.IsAdmin);
            }
            return await _projectMemberQuery.FindCandidates(projectId, dto);
        }

        public async Task<AddProjectMemberResultDto> AddLeaders(int projectId, AddProjectLeaderDto dto)
        {
            _logger.LogInformation($"{nameof(AddLeaders)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var access = await _operationAccessDomainService.EnsureCanManageAsync(projectId, dto.DocumentTypeId, currentUserId, true);
            if (!access.IsAdmin)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorNotProjectLeader);
            }

            var userIds = dto.UserIds.Distinct().ToList();
            var roleNamesByUser = await _projectMemberRepository.GetUserRoleNamesAsync(userIds);
            var existingMemberUserIds = await _projectMemberRepository.GetMemberUserIdsAsync(projectId, dto.DocumentTypeId);

            var now = GetVietnamTime();
            List<ProjectMember> addedMembers = [];
            List<SkippedMemberDto> skipped = [];
            foreach (var userId in userIds)
            {
                var skipReason = _projectMemberDomainService.ResolveSkipReason(userId, RoleConstants.ROLE_LEADER, roleNamesByUser, existingMemberUserIds);
                if (skipReason != null)
                {
                    skipped.Add(new SkippedMemberDto { UserId = userId, Reason = skipReason });
                    continue;
                }
                addedMembers.Add(ProjectMember.Create(projectId, dto.DocumentTypeId, userId, RoleConstants.ROLE_LEADER, null, null, currentUserId, now));
            }
            return await SaveAddedAsync(access, addedMembers, skipped);
        }

        public async Task<AddProjectMemberResultDto> AddMembers(int projectId, AddProjectMemberDto dto)
        {
            _logger.LogInformation($"{nameof(AddMembers)}, projectId={projectId}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.Users.Count == 0 && dto.StaffGroupIds.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, "Phải chọn ít nhất một người hoặc một nhóm");
            }
            if (dto.Users.Any(user => user.Role is not (RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA)))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
            var currentUserId = getCurrentUserId();
            var access = await _operationAccessDomainService.EnsureCanManageAsync(projectId, dto.DocumentTypeId, currentUserId, true);
            var managedByUserId = access.IsAdmin ? dto.ManagedByUserId : access.LeaderUserId;
            if (access.IsAdmin)
            {
                await _projectMemberDomainService.EnsureManagerAsync(projectId, dto.DocumentTypeId, managedByUserId);
            }

            HashSet<string> seenUserIds = [];
            List<(string UserId, string? Role, int? SourceStaffGroupId)> entries = [];
            foreach (var user in dto.Users.Where(user => seenUserIds.Add(user.UserId)))
            {
                entries.Add((user.UserId, user.Role, null));
            }
            foreach (var staffGroupId in dto.StaffGroupIds.Distinct())
            {
                await _staffGroupDomainService.EnsureAccessibleAsync(staffGroupId, currentUserId, access.IsAdmin);
                foreach (var userId in (await _staffGroupRepository.GetMemberUserIdsAsync(staffGroupId)).Where(seenUserIds.Add))
                {
                    entries.Add((userId, null, staffGroupId));
                }
            }
            if (entries.Count > ProjectMemberConst.USERS_PER_REQUEST_MAX)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"Mỗi lần thêm tối đa {ProjectMemberConst.USERS_PER_REQUEST_MAX} người (tính cả người trong nhóm)");
            }

            var roleNamesByUser = await _projectMemberRepository.GetUserRoleNamesAsync(entries.Select(entry => entry.UserId));
            var existingMemberUserIds = await _projectMemberRepository.GetMemberUserIdsAsync(projectId, dto.DocumentTypeId);

            var now = GetVietnamTime();
            List<ProjectMember> addedMembers = [];
            List<SkippedMemberDto> skipped = [];
            foreach (var entry in entries)
            {
                var role = entry.Role ?? _projectMemberDomainService.ResolveGroupMemberRole(roleNamesByUser.GetValueOrDefault(entry.UserId, []));
                var skipReason = _projectMemberDomainService.ResolveSkipReason(entry.UserId, role, roleNamesByUser, existingMemberUserIds);
                if (skipReason != null)
                {
                    skipped.Add(new SkippedMemberDto { UserId = entry.UserId, Reason = skipReason });
                    continue;
                }
                addedMembers.Add(ProjectMember.Create(projectId, dto.DocumentTypeId, entry.UserId, role!, managedByUserId, entry.SourceStaffGroupId, currentUserId, now));
            }
            return await SaveAddedAsync(access, addedMembers, skipped);
        }

        public async Task<ViewProjectMemberDto> Update(int projectId, int memberId, UpdateProjectMemberDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, projectId={projectId}, memberId={memberId}, dto={JsonSerializer.Serialize(dto)}");
            var member = await FindMemberAsync(projectId, memberId);
            var currentUserId = getCurrentUserId();
            var access = await _operationAccessDomainService.EnsureCanManageAsync(member.ProjectId, member.DocumentTypeId, currentUserId, true);
            _operationAccessDomainService.EnsureCanTouchMember(access, member);

            var changesManager = dto.ClearManager || dto.ManagedByUserId != null;
            var managedByUserId = dto.ClearManager ? null : dto.ManagedByUserId;
            if (changesManager)
            {
                if (!access.IsAdmin)
                {
                    throw new UserFriendlyException(ErrorCodes.OperationErrorLeaderForbiddenTarget);
                }
                if (member.Role == RoleConstants.ROLE_LEADER)
                {
                    throw new UserFriendlyException(ErrorCodes.BadRequest, "Leader không có Leader quản lý");
                }
                await _projectMemberDomainService.EnsureManagerAsync(member.ProjectId, member.DocumentTypeId, managedByUserId);
            }

            var oldData = new { member.Id, member.UserId, member.ManagedByUserId, member.IsActive };
            var now = GetVietnamTime();
            var isChanged = false;
            if (changesManager && managedByUserId != member.ManagedByUserId)
            {
                member.ChangeManager(managedByUserId, currentUserId, now);
                isChanged = true;
            }
            if (dto.IsActive.HasValue && dto.IsActive.Value != member.IsActive)
            {
                if (dto.IsActive.Value)
                {
                    member.Activate(currentUserId, now);
                }
                else
                {
                    member.Deactivate(currentUserId, now);
                }
                isChanged = true;
            }

            if (isChanged)
            {
                var user = await _userRepository.FindByIdAsync(member.UserId);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_MEMBER_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE,
                    $"Cập nhật thành viên {user?.UserName ?? member.UserId} của dự án {access.Project.Name} / {access.DocumentType.Name}",
                    oldData, new { member.Id, member.UserId, member.ManagedByUserId, member.IsActive });
                await _unitOfWork.SaveChangesAsync();
            }

            return (await _projectMemberQuery.FindByIds([member.Id])).Single();
        }

        public async Task<RemoveProjectMemberResultDto> Remove(int projectId, int memberId)
        {
            _logger.LogInformation($"{nameof(Remove)}, projectId={projectId}, memberId={memberId}");
            var member = await FindMemberAsync(projectId, memberId);
            var currentUserId = getCurrentUserId();
            var access = await _operationAccessDomainService.EnsureCanManageAsync(member.ProjectId, member.DocumentTypeId, currentUserId, true);
            _operationAccessDomainService.EnsureCanTouchMember(access, member);
            var user = await _userRepository.FindByIdAsync(member.UserId);
            var oldData = new { member.Id, member.UserId, member.Role, member.ManagedByUserId };
            var now = GetVietnamTime();

            RevokeOnRemoveResult revoke;
            List<int> unmanagedMemberIds = [];
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                revoke = await _projectMemberDomainService.RevokeHeldDocumentsAsync(member, currentUserId, now);
                if (member.Role == RoleConstants.ROLE_LEADER)
                {
                    foreach (var managedMember in await _projectMemberRepository.GetManagedMembersAsync(member.ProjectId, member.DocumentTypeId, member.UserId))
                    {
                        managedMember.ChangeManager(null, currentUserId, now);
                        unmanagedMemberIds.Add(managedMember.Id);
                    }
                }
                member.Remove(currentUserId, now);

                AddAuditLog(AuditLogConstants.ACTION_PROJECT_MEMBER_REMOVE, AuditLogConstants.ACTION_TYPE_DELETE,
                    $"Gỡ {user?.UserName ?? member.UserId} khỏi dự án {access.Project.Name} / {access.DocumentType.Name}, thu hồi {revoke.RevokedDocumentCount} văn bản",
                    oldData, new { revoke.RevokedDocumentCount, revoke.CancelledAssignmentTicketIds, revoke.CancelledQaTicketIds, UnmanagedMemberIds = unmanagedMemberIds });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Remove)}, memberId={memberId}");
                await transaction.RollbackAsync();
                throw;
            }

            return new RemoveProjectMemberResultDto
            {
                RevokedDocumentCount = revoke.RevokedDocumentCount,
                KeptDocumentCount = revoke.KeptDocumentCount,
                CancelledAssignmentTicketIds = revoke.CancelledAssignmentTicketIds,
                CancelledQaTicketIds = revoke.CancelledQaTicketIds,
                UnmanagedMemberCount = unmanagedMemberIds.Count,
            };
        }

        private async Task<ProjectMember> FindMemberAsync(int projectId, int memberId)
        {
            var member = await _projectMemberRepository.FindByIdAsync(memberId);
            if (member == null || member.ProjectId != projectId)
            {
                throw new UserFriendlyException(ErrorCodes.OperationErrorMemberNotFound);
            }
            return member;
        }

        private async Task<AddProjectMemberResultDto> SaveAddedAsync(OperationAccess access, List<ProjectMember> addedMembers, List<SkippedMemberDto> skipped)
        {
            if (skipped.Count > 0)
            {
                var fullNames = await _projectMemberQuery.FindFullNames(skipped.Select(item => item.UserId).ToList());
                skipped.ForEach(item => item.FullName = fullNames.GetValueOrDefault(item.UserId));
            }
            if (addedMembers.Count == 0)
            {
                return new AddProjectMemberResultDto { Skipped = skipped };
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _projectMemberRepository.AddRangeAsync(addedMembers);
                AddAuditLog(AuditLogConstants.ACTION_PROJECT_MEMBER_ADD, AuditLogConstants.ACTION_TYPE_CREATE,
                    $"Thêm {addedMembers.Count} thành viên vào dự án {access.Project.Name} / {access.DocumentType.Name}, bỏ qua {skipped.Count}",
                    null, addedMembers.Select(member => new { member.UserId, member.Role, member.ManagedByUserId, member.SourceStaffGroupId }));
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(SaveAddedAsync)}, projectId={access.Project.Id}, documentTypeId={access.DocumentType.Id}");
                await transaction.RollbackAsync();
                throw;
            }

            return new AddProjectMemberResultDto
            {
                AddedCount = addedMembers.Count,
                Added = await _projectMemberQuery.FindByIds(addedMembers.Select(member => member.Id).ToList()),
                Skipped = skipped,
            };
        }
    }
}
