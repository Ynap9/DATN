using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class FindMyAssignmentDto : BaseRequestPagingDto
    {
        public string? Kind { get; set; }
        public int? ProjectId { get; set; }
        public int? DocumentTypeId { get; set; }
        public bool IncludeFinished { get; set; }
    }
}
