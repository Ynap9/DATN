using sh.be.shared.Constants.Mail;
using System.Net;

namespace sh.be.infrastructure.external.Mail.Templates
{
    public static class AccountCreatedMailTemplate
    {
        private const string FILE_NAME = "AccountCreated.html";

        private static readonly Type TemplateType = typeof(AccountCreatedMailTemplate);

        private static readonly string ResourceName = $"{TemplateType.Namespace}.{FILE_NAME}";

        private static readonly Lazy<string> Body = new(() =>
        {
            using var stream = TemplateType.Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new FileNotFoundException(ResourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

        public static string Build(string userName, string password)
        {
            return Body.Value
                .Replace(MailTemplateConstants.USERNAME, WebUtility.HtmlEncode(userName))
                .Replace(MailTemplateConstants.PASSWORD, WebUtility.HtmlEncode(password));
        }
    }
}
