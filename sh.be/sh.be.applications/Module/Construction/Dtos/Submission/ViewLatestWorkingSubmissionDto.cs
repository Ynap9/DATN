namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewLatestWorkingSubmissionDto
    {
        public int SubmissionId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
        public int StatusCheck { get; set; }
        public int StatusQa { get; set; }
        public DateTime? WorkedDate { get; set; }
    }
}
