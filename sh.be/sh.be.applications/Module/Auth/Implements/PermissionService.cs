using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using sh.be.applications.Base;
using sh.be.applications.Module.Auth.Dtos.Permission;
using sh.be.applications.Module.Auth.Interfaces;
using sh.be.infrastructure.data.Persistence.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Implements
{
    public class PermissionService: BaseService, IPermissionService
    {
        public PermissionService(ShDbContext shDbContext, IMapper mapper, ILogger<PermissionService> logger, IHttpContextAccessor httpContextAccessor
        ) : base(shDbContext, mapper, logger, httpContextAccessor)
        {
        }

        public List<GetListPermissionDto> GetListPermission()
        {
            _logger.LogInformation($"{nameof(GetListPermission)}");
            var permission = _shDbContext.Permissions
                .Where(p => !p.Deleted)
                .Select(p => new GetListPermissionDto
                {
                    ParentKey = p.ParentKey ?? "",
                    Key = p.Key,
                }).OrderBy(p => p.Key).ToList();
            return permission;
        }
    }
}
