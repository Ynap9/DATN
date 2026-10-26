using sh.be.shared.HttpRequest.BaseRequest;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class FindStaffGroupCandidateDto : BaseRequestPagingDto
    {
        [Required]
        public string Role { get; set; } = String.Empty;
        public int? ExcludeStaffGroupId { get; set; }
    }
}
