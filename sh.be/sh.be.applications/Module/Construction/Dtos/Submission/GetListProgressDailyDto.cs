namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListProgressDailyDto
    {
        public DateTime Date { get; set; }
        public int CheckedCount { get; set; }
        public int QaPassedCount { get; set; }
    }
}
