using sh.be.applications.Module.Operation.Dtos.Wage;

namespace sh.be.applications.Module.Operation.Interfaces.Services
{
    public interface IWageService
    {
        Task<ViewWagePagingDto> FindPaging(int projectId, FindPagingWageDto dto);
    }
}
