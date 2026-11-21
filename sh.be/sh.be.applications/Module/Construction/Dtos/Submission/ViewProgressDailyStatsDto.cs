namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewProgressDailyStatsDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalChecked { get; set; }
        public int TotalQaPassed { get; set; }
        public List<GetListProgressDailyDto> Days { get; set; } = new();
    }
}
