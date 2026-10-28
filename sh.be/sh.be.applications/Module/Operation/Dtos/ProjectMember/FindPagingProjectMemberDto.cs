using sh.be.shared.HttpRequest.BaseRequest;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class FindPagingProjectMemberDto : BaseRequestPagingDto
    {
        [Required]
        public int DocumentTypeId { get; set; }
        public string? Role { get; set; }
        public string? ManagedByUserId { get; set; }
        public bool? IsActive { get; set; }
    }
}
