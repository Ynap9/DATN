namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListSubmissionLogDto
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int OldStatus { get; set; }
        public int NewStatus { get; set; }
        public string Description { get; set; } = String.Empty;
        public string? Note { get; set; }
        public int ChangedDataCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
