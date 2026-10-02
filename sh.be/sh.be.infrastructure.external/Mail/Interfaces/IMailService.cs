namespace sh.be.infrastructure.external.Mail.Interfaces
{
    public interface IMailService
    {
        Task SendAsync(string toAddress, string subject, string body);
    }
}
