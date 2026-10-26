using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class FindPagingStaffGroupDto : BaseRequestPagingDto
    {
        public string? OwnerUserId { get; set; }
        public string? Role { get; set; }
    }
}
