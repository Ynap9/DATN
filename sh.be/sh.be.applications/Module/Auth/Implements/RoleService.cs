using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces.Api;
using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.applications.Module.Auth.Interfaces.Queries;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Entities;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Core.Repositories;
using sh.be.shared.Constants.Core;
using sh.be.shared.HttpRequest.BaseRequest;
using sh.be.shared.HttpRequest.Error;
using sh.be.shared.HttpRequest.Exception;
using sh.be.shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Implements
{
    public class RoleService : BaseService, IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRoleQuery _roleQuery;
        private readonly RoleDomainService _roleDomainService;

        public RoleService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<RoleService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IRoleRepository roleRepository, IPermissionRepository permissionRepository, IRoleQuery roleQuery, RoleDomainService roleDomainService
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _roleQuery = roleQuery;
            _roleDomainService = roleDomainService;
        }

        public List<GetListRoleDropDownDto> GetRoleDropDown()
        {
            _logger.LogInformation($"{nameof(GetRoleDropDown)}");
            return _roleQuery.GetRoleDropDown();
        }

        public FindRoleByIdDto FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)} id ={JsonSerializer.Serialize(id)}");
            var role = _roleQuery.FindById(id);
            if (role == null) {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }
            return role;
        }

        public BaseResponsePagingDto<GetListRoleDto> FindPaging(FindPagingRoleDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)} , dto ={JsonSerializer.Serialize(dto)}");
            return _roleQuery.FindPaging(dto);
        }

        public List<GetRolePermissionTreeDto> GetPermissionTree(int roleId)
        {
            _logger.LogInformation($"{nameof(GetPermissionTree)} roleId ={JsonSerializer.Serialize(roleId)}");
            if (!_roleRepository.Exists(roleId))
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }

            var permissions = _permissionRepository.GetActive();
            var grantedKeys = PermissionGrantPolicy.ResolveGrantedKeys(permissions, _roleRepository.GetPermissionKeys(roleId));

            var nodes = permissions
                .Select(p => new GetRolePermissionTreeDto
                {
                    Key = p.Key,
                    ParentKey = p.ParentKey,
                    Description = p.Description,
                    Module = p.Module,
                    IsChecked = grantedKeys.Contains(p.Key),
                }).ToList();

            return TreeUtils.BuildTree(nodes, p => p.Key, p => p.ParentKey, p => p.Children);
        }

        public async Task UpdatePermission(UpdateRolePermissionDto dto)
        {
            _logger.LogInformation($"{nameof(UpdatePermission)}, dto = {JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var role = await _roleRepository.FindByIdAsync(dto.RoleId)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);

            var oldPermissionKeys = await _roleRepository.GetPermissionKeysAsync(dto.RoleId);

            var checkedKeys = TreeUtils.Flatten(dto.Permissions, p => p.Children)
                .Where(p => p.IsChecked)
                .Select(p => p.Key)
                .Distinct()
                .ToList();

            var permissions = await _permissionRepository.GetActiveAsync();
            var rolePermissions = role.AssignPermissions(permissions, checkedKeys, currentUserId, now);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _roleRepository.ReplacePermissionsAsync(dto.RoleId, rolePermissions);
                AddAuditLog(AuditLogConstants.ACTION_ROLE_UPDATE_PERMISSION, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật quyền của vai trò có Id = {dto.RoleId}",
                    new { dto.RoleId, PermissionKeys = oldPermissionKeys }, new { dto.RoleId, PermissionKeys = rolePermissions.Select(p => p.PermissionKey) });
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(UpdatePermission)}, roleId={dto.RoleId}");
                await transaction.RollbackAsync();
                throw;
            }
        }


        public void Create(CreateRoleDto dto)
        {
            _logger.LogInformation($"{nameof(Create)} ,dto = {JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();
            _roleDomainService.EnsureNotExists(dto.Id);

            var role = Role.Create(dto.Name, dto.Description, currentUserId, now, dto.Id);

            _roleRepository.Add(role);
            AddAuditLog(AuditLogConstants.ACTION_ROLE_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo vai trò {role.Name}",
                null, new { role.Name, role.Description });
            _unitOfWork.SaveChanges();
        }

        public void Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id = {JsonSerializer.Serialize(id)}");

            var currentUserId = getCurrentUserId();
            var vietnamTime = GetVietnamTime();
            var role = _roleRepository.FindById(id)
                ??throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);

            role.Delete(currentUserId, vietnamTime);

            _roleRepository.Update(role);
            AddAuditLog(AuditLogConstants.ACTION_ROLE_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa vai trò {role.Name}",
                new { role.Id, role.Name, role.Description }, null);
            _unitOfWork.SaveChanges();
        }


        public void Update (int id, UpdateRoleDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, dto ={JsonSerializer.Serialize(dto)}, id ={JsonSerializer.Serialize(id)}");
            var currentUserId = getCurrentUserId();
            var vietnamTime = GetVietnamTime();
            var role = _roleRepository.FindById(id)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            var oldData = new { role.Id, role.Name, role.Description };

            role.Update(dto.Name, dto.Description, currentUserId, vietnamTime);

            _roleRepository.Update(role);
            AddAuditLog(AuditLogConstants.ACTION_ROLE_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật vai trò {role.Name}",
                oldData, new { role.Id, role.Name, role.Description });
            _unitOfWork.SaveChanges();
        }
    }
}
