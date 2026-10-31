using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sh.be.applications.Module.Auth.Dtos.User
{
    public class FindPagingUserDto: BaseRequestPagingDto
    {
        [FromQuery(Name = "roleId")]
        public int? RoleId { get; set; }
    }
}
