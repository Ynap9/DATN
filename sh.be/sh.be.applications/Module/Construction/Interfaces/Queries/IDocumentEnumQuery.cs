using sh.be.applications.Module.Construction.Dtos.DocumentEnum;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface IDocumentEnumQuery
    {
        Task<List<GetListDocumentEnumDto>> GetList(int documentTypeId);
    }
}
