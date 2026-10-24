namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewQaSubmissionDetailDto : ViewSubmissionDetailDto
    {
        public string? QaBy { get; set; }
        public string? QaUserName { get; set; }
        public DateTime? QaDate { get; set; }
        public string? CheckedBy { get; set; }
        public string? CheckedUserName { get; set; }
        public DateTime? CheckedDate { get; set; }
        public string? CheckNote { get; set; }
    }
}
