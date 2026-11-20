using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class FindPagingProjectDto : BaseRequestPagingDto
    {
        public int? Status { get; set; }
        public int? DocumentTypeId { get; set; }
        public string? CreatorKeyword { get; set; }
        public DateTime? StartedFrom { get; set; }
        public DateTime? StartedTo { get; set; }
        public DateTime? EndedFrom { get; set; }
        public DateTime? EndedTo { get; set; }
    }
}
