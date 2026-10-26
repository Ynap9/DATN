using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Operation.Dtos.StaffGroup;
using sh.be.applications.Module.Operation.Interfaces.Queries;
using sh.be.applications.Module.Operation.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Core.Repositories;
using sh.be.domain.Module.Operation.ProjectMembers.Services;
using sh.be.domain.Module.Operation.StaffGroups.Entities;
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
    public class StaffGroupService : BaseService, IStaffGroupService
    {
        private readonly IStaffGroupRepository _staffGroupRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IStaffGroupQuery _staffGroupQuery;
        private readonly StaffGroupDomainService _staffGroupDomainService;

        public StaffGroupService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<StaffGroupService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IStaffGroupRepository staffGroupRepository, IRoleRepository roleRepository, IStaffGroupQuery staffGroupQuery, StaffGroupDomainService staffGroupDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _staffGroupRepository = staffGroupRepository;
            _roleRepository = roleRepository;
            _staffGroupQuery = staffGroupQuery;
            _staffGroupDomainService = staffGroupDomainService;
        }

        public async Task<BaseResponsePagingDto<ViewStaffGroupDto>> FindPaging(FindPagingStaffGroupDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > StaffGroupConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {StaffGroupConst.MAX_PAGE_SIZE}");
            }
            if (dto.Role is not (null or RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
            var currentUserId = getCurrentUserId();
            if (!OperationAccessPolicy.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId)))
            {
                dto.OwnerUserId = currentUserId;
            }
            return await _staffGroupQuery.FindPaging(dto);
        }

        public async Task<FindStaffGroupByIdDto> FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)}, id={id}");
            await EnsureAccessibleAsync(id);
            return await _staffGroupQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNotFound);
        }

        public async Task<FindStaffGroupByIdDto> Create(CreateStaffGroupDto dto)
        {
            _logger.LogInformation($"{nameof(Create)}, dto={JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var name = dto.Name.Trim();
            await _staffGroupDomainService.EnsureUniqueNameAsync(currentUserId, name, null);

            var userIds = dto.UserIds.Distinct().ToList();
            if (userIds.Count > 0)
            {
                await _staffGroupDomainService.EnsureSameStaffRoleAsync(userIds, []);
            }

            var now = GetVietnamTime();
            var staffGroup = StaffGroup.Create(name, dto.Description, currentUserId, currentUserId, now);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _staffGroupRepository.AddAsync(staffGroup);
                await _unitOfWork.SaveChangesAsync();

                await _staffGroupRepository.AddMembersAsync(userIds.Select(userId => StaffGroupMember.Create(staffGroup.Id, userId, currentUserId, now)));
                AddAuditLog(AuditLogConstants.ACTION_STAFF_GROUP_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo nhóm nhân viên {staffGroup.Name} ({userIds.Count} người)",
                    null, new { staffGroup.Id, staffGroup.Name, staffGroup.Description, staffGroup.OwnerUserId, UserIds = userIds });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(Create)}, name={name}");
                await transaction.RollbackAsync();
                throw;
            }

            return await _staffGroupQuery.FindById(staffGroup.Id)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNotFound);
        }

        public async Task Update(int id, UpdateStaffGroupDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var staffGroup = await EnsureAccessibleAsync(id);
            var oldData = new { staffGroup.Id, staffGroup.Name, staffGroup.Description };

            var name = dto.Name.Trim();
            await _staffGroupDomainService.EnsureUniqueNameAsync(staffGroup.OwnerUserId, name, id);
            staffGroup.Update(name, dto.Description, getCurrentUserId(), GetVietnamTime());

            AddAuditLog(AuditLogConstants.ACTION_STAFF_GROUP_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật nhóm nhân viên {staffGroup.Name}",
                oldData, new { staffGroup.Id, staffGroup.Name, staffGroup.Description });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id={id}");
            var staffGroup = await EnsureAccessibleAsync(id);
            var memberUserIds = await _staffGroupRepository.GetMemberUserIdsAsync(id);

            await _staffGroupRepository.RemoveAllMembersAsync(id);
            staffGroup.Delete(getCurrentUserId(), GetVietnamTime());

            AddAuditLog(AuditLogConstants.ACTION_STAFF_GROUP_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa nhóm nhân viên {staffGroup.Name} ({memberUserIds.Count} người)",
                new { staffGroup.Id, staffGroup.Name, staffGroup.Description, staffGroup.OwnerUserId, UserIds = memberUserIds }, null);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<FindStaffGroupByIdDto> AddMembers(int id, ChangeStaffGroupMembersDto dto)
        {
            _logger.LogInformation($"{nameof(AddMembers)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var staffGroup = await EnsureAccessibleAsync(id);
            var existingUserIds = await _staffGroupRepository.GetMemberUserIdsAsync(id);
            var newUserIds = dto.UserIds.Distinct().Except(existingUserIds).ToList();

            if (newUserIds.Count > 0)
            {
                await _staffGroupDomainService.EnsureSameStaffRoleAsync(newUserIds, existingUserIds);

                var currentUserId = getCurrentUserId();
                var now = GetVietnamTime();
                await _staffGroupRepository.AddMembersAsync(newUserIds.Select(userId => StaffGroupMember.Create(id, userId, currentUserId, now)));
                AddAuditLog(AuditLogConstants.ACTION_STAFF_GROUP_ADD_MEMBERS, AuditLogConstants.ACTION_TYPE_CREATE, $"Thêm {newUserIds.Count} người vào nhóm nhân viên {staffGroup.Name}",
                    null, new { staffGroup.Id, UserIds = newUserIds });
                await _unitOfWork.SaveChangesAsync();
            }

            return await _staffGroupQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNotFound);
        }

        public async Task<FindStaffGroupByIdDto> RemoveMembers(int id, ChangeStaffGroupMembersDto dto)
        {
            _logger.LogInformation($"{nameof(RemoveMembers)}, id={id}, dto={JsonSerializer.Serialize(dto)}");
            var staffGroup = await EnsureAccessibleAsync(id);
            var existingUserIds = await _staffGroupRepository.GetMemberUserIdsAsync(id);
            var removedUserIds = dto.UserIds.Distinct().Intersect(existingUserIds).ToList();

            if (removedUserIds.Count > 0)
            {
                await _staffGroupRepository.RemoveMembersAsync(id, removedUserIds);
                AddAuditLog(AuditLogConstants.ACTION_STAFF_GROUP_REMOVE_MEMBERS, AuditLogConstants.ACTION_TYPE_DELETE, $"Bỏ {removedUserIds.Count} người khỏi nhóm nhân viên {staffGroup.Name}",
                    new { staffGroup.Id, UserIds = removedUserIds }, null);
                await _unitOfWork.SaveChangesAsync();
            }

            return await _staffGroupQuery.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.OperationErrorStaffGroupNotFound);
        }

        public async Task<BaseResponsePagingDto<StaffGroupCandidateDto>> FindCandidates(FindStaffGroupCandidateDto dto)
        {
            _logger.LogInformation($"{nameof(FindCandidates)}, dto={JsonSerializer.Serialize(dto)}");
            if (dto.PageSize <= 0 || dto.PageSize > StaffGroupConst.MAX_PAGE_SIZE)
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"pageSize phải từ 1 đến {StaffGroupConst.MAX_PAGE_SIZE}");
            }
            if (dto.Role is not (RoleConstants.ROLE_DATA or RoleConstants.ROLE_QA))
            {
                throw new UserFriendlyException(ErrorCodes.BadRequest, $"role phải là {RoleConstants.ROLE_DATA} hoặc {RoleConstants.ROLE_QA}");
            }
            return await _staffGroupQuery.FindCandidates(dto);
        }

        private async Task<StaffGroup> EnsureAccessibleAsync(int id)
        {
            var currentUserId = getCurrentUserId();
            var isAdmin = OperationAccessPolicy.IsAdmin(await _roleRepository.GetRoleNamesByUserIdAsync(currentUserId));
            return await _staffGroupDomainService.EnsureAccessibleAsync(id, currentUserId, isAdmin);
        }
    }
}
