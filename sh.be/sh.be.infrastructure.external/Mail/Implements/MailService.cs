using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using sh.be.infrastructure.external.Mail.Interfaces;
using sh.be.shared.Settings;
using System.Net;
using System.Net.Mail;

namespace sh.be.infrastructure.external.Mail.Implements
{
    public class MailService : IMailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<MailService> _logger;

        public MailService(IOptions<EmailSettings> emailSettings, ILogger<MailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toAddress, string subject, string body)
        {
            _logger.LogInformation($"{nameof(SendAsync)}, toAddress={toAddress}, subject={subject}");
            var smtp = _emailSettings.Smtp;

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.EnableSsl,
                UseDefaultCredentials = smtp.UseDefaultCredentials,
            };
            if (!smtp.UseDefaultCredentials)
            {
                client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password, smtp.Domain);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.DefaultFromAddress, _emailSettings.DefaultFromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            message.To.Add(toAddress);

            await client.SendMailAsync(message);
        }
    }
}
