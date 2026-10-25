namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewQaStatsSummaryDto : QaStatsMetricDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int ChargeableFieldCount { get; set; }
    }
}
