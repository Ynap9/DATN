using AutoMapper;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Base.Interfaces;
using sh.be.applications.Module.Auth.Dtos.Permission;
using sh.be.applications.Module.Auth.Interfaces.Services;
using sh.be.domain.Base;
using sh.be.domain.Module.Auth.Repositories;
using sh.be.domain.Module.Auth.Services;
using sh.be.domain.Module.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Implements
{
    public class PermissionService: BaseService, IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRoleRepository _roleRepository;

        public PermissionService(IUnitOfWork unitOfWork, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<PermissionService> logger, IRequestContext requestContext, TimeProvider timeProvider,
            IPermissionRepository permissionRepository, IRoleRepository roleRepository
        ) : base(unitOfWork, auditLogRepository, mapper, logger, requestContext, timeProvider)
        {
            _permissionRepository = permissionRepository;
            _roleRepository = roleRepository;
        }

        public List<GetListPermissionDto> GetListPermission()
        {
            _logger.LogInformation($"{nameof(GetListPermission)}");
            var permission = _permissionRepository.GetActive()
                .Select(p => new GetListPermissionDto
                {
                    ParentKey = p.ParentKey ?? "",
                    Key = p.Key,
                }).ToList();
            return permission;
        }

        public async Task<bool> HasAnyPermission(string userId, string[] permissionKeys)
        {
            var roleNames = await _roleRepository.GetRoleNamesByUserIdAsync(userId);
            if (PermissionGrantPolicy.IsSuperAdmin(roleNames))
            {
                return true;
            }

            var grantedKeys = await _roleRepository.GetPermissionKeysByUserIdAsync(userId);
            var permissions = await _permissionRepository.GetActiveAsync();
            return PermissionGrantPolicy.HasAnyPermission(permissions, grantedKeys, permissionKeys);
        }
    }
}
