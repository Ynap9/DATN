using sh.be.applications.Module.Construction.Dtos.DocumentEnum;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface IDocumentEnumService
    {
        Task<List<GetListDocumentEnumDto>> GetList(int documentTypeId);
        Task<int> Create(int documentTypeId, CreateDocumentEnumDto dto);
        Task Update(int documentTypeId, int enumId, UpdateDocumentEnumDto dto);
        Task Delete(int documentTypeId, int enumId);
    }
}
