using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class FindPagingAiProviderDto : BaseRequestPagingDto
    {
        public string? Protocol { get; set; }
        public string? Scope { get; set; }
        public bool? IsActive { get; set; }
    }
}
