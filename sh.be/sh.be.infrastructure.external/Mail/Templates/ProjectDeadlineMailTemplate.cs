using sh.be.infrastructure.external.Mail.Base;
using sh.be.shared.Constants.Mail;
using System.Net;

namespace sh.be.infrastructure.external.Mail.Templates
{
    public static class ProjectDeadlineMailTemplate
    {
        private const string FILE_NAME = "ProjectDeadline.html";

        private static readonly Lazy<string> Body = new(() => MailTemplateLoader.Load(typeof(ProjectDeadlineMailTemplate), FILE_NAME));

        public static string Build(string projectName, DateTime endedAt, int daysLeft, bool isExpired)
        {
            var deadlineStatus = isExpired
                ? "đã hết hạn"
                : daysLeft == 0 ? "hết hạn vào hôm nay" : $"còn {daysLeft} ngày nữa là hết hạn";
            return Body.Value
                .Replace(MailTemplateConstants.PROJECT_NAME, WebUtility.HtmlEncode(projectName))
                .Replace(MailTemplateConstants.ENDED_AT, endedAt.ToString("dd/MM/yyyy"))
                .Replace(MailTemplateConstants.DEADLINE_STATUS, deadlineStatus);
        }
    }
}
