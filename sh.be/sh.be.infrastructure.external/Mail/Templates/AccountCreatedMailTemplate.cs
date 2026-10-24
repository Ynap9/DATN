using sh.be.infrastructure.external.Mail.Base;
using sh.be.shared.Constants.Mail;
using System.Net;

namespace sh.be.infrastructure.external.Mail.Templates
{
    public static class AccountCreatedMailTemplate
    {
        private const string FILE_NAME = "AccountCreated.html";

        private static readonly Lazy<string> Body = new(() => MailTemplateLoader.Load(typeof(AccountCreatedMailTemplate), FILE_NAME));

        public static string Build(string userName, string password)
        {
            return Body.Value
                .Replace(MailTemplateConstants.USERNAME, WebUtility.HtmlEncode(userName))
                .Replace(MailTemplateConstants.PASSWORD, WebUtility.HtmlEncode(password));
        }
    }
}
