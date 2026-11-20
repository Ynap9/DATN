using sh.be.applications.Module.Ai.Dtos.PromptTemplate;

namespace sh.be.applications.Module.Ai.Interfaces.Queries
{
    public interface IPromptTemplateQuery
    {
        Task<ViewPromptEditorDto> FindEditor(int documentTypeId);
        Task<List<GetListPromptTemplateVersionDto>> FindVersions(int documentTypeId);
        Task<List<string>> GetDocumentFieldNames(List<int> documentFieldIds);
    }
}
