using sh.be.applications.Base.Dtos.BackgroundJob;

namespace sh.be.applications.Module.Construction.Dtos.ProjectDeadline
{
    public class ViewDeadlineCheckStatusDto
    {
        public RecurringJobInfoDto? Job { get; set; }
        public int WarningDays { get; set; }
        public bool MailEnabled { get; set; }
        public List<string> Recipients { get; set; } = [];
    }
}
