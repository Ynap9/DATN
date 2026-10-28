using sh.be.shared.HttpRequest.BaseRequest;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class FindProjectMemberCandidateDto : BaseRequestPagingDto
    {
        [Required]
        public int DocumentTypeId { get; set; }
        [Required]
        public string Role { get; set; } = String.Empty;
        public int? StaffGroupId { get; set; }
    }
}
