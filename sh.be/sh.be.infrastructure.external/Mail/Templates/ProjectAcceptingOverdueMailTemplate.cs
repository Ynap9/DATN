using sh.be.infrastructure.external.Mail.Base;
using sh.be.shared.Constants.Mail;
using System.Net;

namespace sh.be.infrastructure.external.Mail.Templates
{
    public static class ProjectAcceptingOverdueMailTemplate
    {
        private const string FILE_NAME = "ProjectAcceptingOverdue.html";

        private static readonly Lazy<string> Body = new(() => MailTemplateLoader.Load(typeof(ProjectAcceptingOverdueMailTemplate), FILE_NAME));

        // daysLeft < 0 khi dự án đang nghiệm thu quá hạn, số ngày quá hạn = -daysLeft
        public static string Build(string projectName, DateTime endedAt, int daysLeft)
        {
            return Body.Value
                .Replace(MailTemplateConstants.PROJECT_NAME, WebUtility.HtmlEncode(projectName))
                .Replace(MailTemplateConstants.ENDED_AT, endedAt.ToString("dd/MM/yyyy"))
                .Replace(MailTemplateConstants.OVERDUE_DAYS, (-daysLeft).ToString());
        }
    }
}
