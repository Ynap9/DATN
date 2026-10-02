using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Module.Auth.Dtos.Role;
using sh.be.applications.Module.Auth.Interfaces;
using sh.be.domain.Module.Auth;
using sh.be.infrastructure.data.Persistence.Db;
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
        public RoleService(ShDbContext shDbContext, IMapper mapper, ILogger<RoleService> logger, IHttpContextAccessor httpContextAccessor
        ) : base(shDbContext, mapper, logger, httpContextAccessor)
        {
        }

        public List<GetListRoleDropDownDto> GetRoleDropDown()
        {
            _logger.LogInformation($"{nameof(GetRoleDropDown)}");
            var roles = _shDbContext.Roles
                .Where(r => !r.Deleted)
                .Select(r => new GetListRoleDropDownDto
                {
                    Id = r.Id,
                    Name = r.Name,
                }).ToList();
            return roles;
        }

        public FindRoleByIdDto FindById(int id)
        {
            _logger.LogInformation($"{nameof(FindById)} id ={JsonSerializer.Serialize(id)}");
            var role = _shDbContext.Roles
                .Where(r => r.Id == id && !r.Deleted)
                .AsNoTracking()
                .Select(r => new FindRoleByIdDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                }).FirstOrDefault();
            if (role == null) {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }
            return role;
        }
        

        public BaseResponsePagingDto<GetListRoleDto> FindPaging(FindPagingRoleDto dto)
        {
            _logger.LogInformation($"{nameof(FindPaging)} , dto ={JsonSerializer.Serialize(dto)}");
            var query = _shDbContext.Roles.AsNoTracking().Where(r => !r.Deleted);
            if (!string.IsNullOrEmpty(dto.Keyword))
            {
                query = query.Where(r => r.Name!.Contains(dto.Keyword) && !r.Deleted);
            }

            var totalCount = query.Count();
            var roles = query
                .Paging(dto)
                .AsNoTracking()
                .ToList();
            
            
            
            var items = roles.Select(r => new GetListRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
               

            });

            return new BaseResponsePagingDto<GetListRoleDto>
            {
                Items = items,
                TotalItems = totalCount,
            };



        }

        public List<GetRolePermissionTreeDto> GetPermissionTree(int roleId)
        {
            _logger.LogInformation($"{nameof(GetPermissionTree)} roleId ={JsonSerializer.Serialize(roleId)}");
            var isRoleExist = _shDbContext.Roles.Any(r => r.Id == roleId && !r.Deleted);
            if (!isRoleExist)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }

            var permissions = _shDbContext.Permissions
                .AsNoTracking()
                .Where(p => !p.Deleted)
                .OrderBy(p => p.Key)
                .Select(p => new GetRolePermissionTreeDto
                {
                    Key = p.Key,
                    ParentKey = p.ParentKey,
                    Description = p.Description,
                    Module = p.Module,
                    IsChecked = _shDbContext.RolePermissions.Any(rp => rp.RoleId == roleId && rp.PermissionKey == p.Key && !rp.Deleted),
                }).ToList();

            var tree = TreeUtils.BuildTree(permissions, p => p.Key, p => p.ParentKey, p => p.Children);
            TreeUtils.SyncChecked(tree, p => p.Children, p => p.IsChecked, (p, isChecked) => p.IsChecked = isChecked);
            return tree;
        }

        public async Task UpdatePermission(UpdateRolePermissionDto dto)
        {
            _logger.LogInformation($"{nameof(UpdatePermission)}, dto = {JsonSerializer.Serialize(dto)}");
            var currentUserId = getCurrentUserId();
            var now = GetVietnamTime();

            var isRoleExist = await _shDbContext.Roles.AnyAsync(r => r.Id == dto.RoleId && !r.Deleted);
            if (!isRoleExist)
            {
                throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            }

            var oldPermissionKeys = await _shDbContext.RolePermissions
                .Where(rp => rp.RoleId == dto.RoleId && !rp.Deleted)
                .Select(rp => rp.PermissionKey)
                .ToListAsync();

            var checkedKeys = TreeUtils.Flatten(dto.Permissions, p => p.Children)
                .Where(p => p.IsChecked)
                .Select(p => p.Key)
                .Distinct()
                .ToList();

            var permissions = await _shDbContext.Permissions
                .AsNoTracking()
                .Where(p => !p.Deleted)
                .Select(p => new GetRolePermissionTreeDto
                {
                    Key = p.Key,
                    ParentKey = p.ParentKey,
                    IsChecked = checkedKeys.Contains(p.Key),
                }).ToListAsync();

            var tree = TreeUtils.BuildTree(permissions, p => p.Key, p => p.ParentKey, p => p.Children);
            TreeUtils.SyncChecked(tree, p => p.Children, p => p.IsChecked, (p, isChecked) => p.IsChecked = isChecked);

            var checkedPermissions = TreeUtils.Flatten(tree, p => p.Children)
                .Where(p => p.IsChecked)
                .ToList();

            await using var transaction = await _shDbContext.Database.BeginTransactionAsync();
            try
            {
                await _shDbContext.RolePermissions
                    .Where(rp => rp.RoleId == dto.RoleId)
                    .ExecuteDeleteAsync();

                _shDbContext.RolePermissions.AddRange(checkedPermissions.Select(p => new RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionParentKey = p.ParentKey ?? String.Empty,
                    PermissionKey = p.Key,
                    CreatedBy = currentUserId,
                    CreatedDate = now,
                }));
                AddAuditLog(AuditLogConstants.ACTION_ROLE_UPDATE_PERMISSION, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật quyền của vai trò có Id = {dto.RoleId}",
                    new { dto.RoleId, PermissionKeys = oldPermissionKeys }, new { dto.RoleId, PermissionKeys = checkedPermissions.Select(p => p.Key) });
                await _shDbContext.SaveChangesAsync();

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
            var roleExisted = _shDbContext.Roles
                .Where(r => r.Id == dto.Id && !r.Deleted)
                .Any();
            if (roleExisted) {
                throw new UserFriendlyException(ErrorCodes.Found, "Role đã tồn tại");
            }
            else
            {
                var role = new Role
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    IsSystem = 1,
                    CreatedBy = currentUserId,
                    CreatedDate = now,
                };

                _shDbContext.Roles.Add(role);
                AddAuditLog(AuditLogConstants.ACTION_ROLE_CREATE, AuditLogConstants.ACTION_TYPE_CREATE, $"Tạo vai trò {role.Name}",
                    null, new { role.Name, role.Description });
                _shDbContext.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            _logger.LogInformation($"{nameof(Delete)}, id = {JsonSerializer.Serialize(id)}");

            var currentUserId = getCurrentUserId();
            var vietnamTime = GetVietnamTime();
            var role = _shDbContext.Roles.FirstOrDefault(r => r.Id == id && !r.Deleted)
                ??throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);

            role.Deleted = true;
            role.DeletedDate = vietnamTime;
            role.DeletedBy = currentUserId;

            _shDbContext.Roles.Update(role);
            AddAuditLog(AuditLogConstants.ACTION_ROLE_DELETE, AuditLogConstants.ACTION_TYPE_DELETE, $"Xóa vai trò {role.Name}",
                new { role.Id, role.Name, role.Description }, null);
            _shDbContext.SaveChanges();
        }


        public void Update (int id, UpdateRoleDto dto)
        {
            _logger.LogInformation($"{nameof(Update)}, dto ={JsonSerializer.Serialize(dto)}, id ={JsonSerializer.Serialize(id)}");
            var currentUserId = getCurrentUserId();
            var vietnamTime = GetVietnamTime();
            var role = _shDbContext.Roles.FirstOrDefault(r => r.Id == id && !r.Deleted)
                ?? throw new UserFriendlyException(ErrorCodes.AuthErrorRoleNotFound);
            var oldData = new { role.Id, role.Name, role.Description };

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.ModifiedDate = vietnamTime;
            role.ModifiedBy = currentUserId;


            _shDbContext.Roles.Update(role);
            AddAuditLog(AuditLogConstants.ACTION_ROLE_UPDATE, AuditLogConstants.ACTION_TYPE_UPDATE, $"Cập nhật vai trò {role.Name}",
                oldData, new { role.Id, role.Name, role.Description });
            _shDbContext.SaveChanges();
        }
    }
}
