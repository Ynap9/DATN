namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListQaEntryUserStatsDto : QaStatsMetricDto
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
    }
}
