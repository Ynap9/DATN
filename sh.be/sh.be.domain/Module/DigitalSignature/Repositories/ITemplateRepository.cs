using sh.be.domain.Module.DigitalSignature.Entities;

namespace sh.be.domain.Module.DigitalSignature.Repositories
{
    public interface ITemplateRepository
    {
        Task<Template?> FindByIdAsync(int id);
        void Add(Template template);
        void Update(Template template);
        Task<List<TemplatePosition>> GetPositionsAsync(int templateId);
        Task ReplacePositionsAsync(int templateId, List<TemplatePosition> positions);
    }
}
