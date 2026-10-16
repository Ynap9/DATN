using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class FindPagingProjectDto : BaseRequestPagingDto
    {
        public int? Status { get; set; }
    }
}
