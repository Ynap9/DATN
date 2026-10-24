namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListQaSubmissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public string FileUrlS3Object { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
        public int StatusCheck { get; set; }
        public int StatusQa { get; set; }
        public string? QaReason { get; set; }
        public string? QaBy { get; set; }
        public string? QaUserName { get; set; }
        public DateTime? QaDate { get; set; }
        public string? CheckedBy { get; set; }
        public string? CheckedUserName { get; set; }
        public DateTime? CheckedDate { get; set; }
    }
}
