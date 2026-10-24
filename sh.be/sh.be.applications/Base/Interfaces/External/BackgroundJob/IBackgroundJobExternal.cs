using sh.be.applications.Base.Dtos.BackgroundJob;

namespace sh.be.applications.Base.Interfaces.External.BackgroundJob
{
    public interface IBackgroundJobExternal
    {
        BackgroundJobConnectionDto CheckConnection();
    }
}
