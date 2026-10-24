using sh.be.applications.Base.Dtos.BackgroundJob;

namespace sh.be.applications.Module.Core.Interfaces.Services
{
    public interface IBackgroundJobService
    {
        BackgroundJobConnectionDto CheckConnection();
    }
}
