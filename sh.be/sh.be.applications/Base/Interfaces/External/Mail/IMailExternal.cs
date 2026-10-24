namespace sh.be.applications.Base.Interfaces.External.Mail
{
    public interface IMailExternal
    {
        Task SendAsync(string toAddress, string subject, string body);
        Task SendAccountCreatedAsync(string toAddress, string userName, string password);
    }
}
