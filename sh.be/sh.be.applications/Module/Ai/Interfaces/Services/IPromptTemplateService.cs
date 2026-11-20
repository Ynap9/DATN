using sh.be.applications.Module.Ai.Dtos.PromptTemplate;

namespace sh.be.applications.Module.Ai.Interfaces.Services
{
    public interface IPromptTemplateService
    {
        Task<ViewPromptEditorDto> FindEditor(int documentTypeId);
        Task<List<GetListPromptTemplateVersionDto>> FindVersions(int documentTypeId);
        Task<int> SaveDraft(int documentTypeId, SavePromptDraftDto dto);
        Task Publish(int id, PublishPromptTemplateDto dto);
        Task<int> Restore(int id);
        Task Discard(int id);
        Task<ViewComposedPromptDto> Preview(int id);
    }
}
