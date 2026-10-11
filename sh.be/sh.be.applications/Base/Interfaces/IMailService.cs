namespace sh.be.applications.Base.Interfaces
{
    public interface IMailService
    {
        Task SendAsync(string toAddress, string subject, string body);
        Task SendAccountCreatedAsync(string toAddress, string userName, string password);
    }
}
