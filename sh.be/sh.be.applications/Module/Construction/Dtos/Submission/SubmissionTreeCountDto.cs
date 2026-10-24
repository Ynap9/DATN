namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionTreeCountDto
    {
        public int Total { get; set; }
        public int TotalNotChecked { get; set; }
        public int TotalChecked { get; set; }
        public int TotalReturned { get; set; }
        public int TotalQaPassed { get; set; }
        public int TotalQaFailed { get; set; }
    }
}
