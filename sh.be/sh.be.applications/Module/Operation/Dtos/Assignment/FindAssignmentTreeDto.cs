using sh.be.shared.HttpRequest.BaseRequest;
using System.ComponentModel.DataAnnotations;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class FindAssignmentTreeDto : BaseRequestPagingDto
    {
        [Required]
        public string Role { get; set; } = String.Empty;
        public bool OnlyUnassigned { get; set; }
        public string? HolderUserId { get; set; }
    }
}
