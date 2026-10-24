namespace sh.be.applications.Base.Dtos.BackgroundJob
{
    public class RecurringJobInfoDto
    {
        public string Id { get; set; } = String.Empty;
        public string Cron { get; set; } = String.Empty;
        public string? TimeZoneId { get; set; }
        public DateTime? NextExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public string? LastJobId { get; set; }
        public string? LastJobState { get; set; }
        public string? Error { get; set; }
    }
}
