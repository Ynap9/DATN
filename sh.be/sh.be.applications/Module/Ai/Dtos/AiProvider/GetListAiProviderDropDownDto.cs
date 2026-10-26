namespace sh.be.applications.Module.Ai.Dtos.AiProvider
{
    public class GetListAiProviderDropDownDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Protocol { get; set; } = String.Empty;
        public string Scope { get; set; } = String.Empty;
        public bool HasApiKey { get; set; }
    }
}
