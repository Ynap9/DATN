namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListSubmissionQaLogDto
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int OldStatusQa { get; set; }
        public int NewStatusQa { get; set; }
        public int OldStatusCheck { get; set; }
        public int NewStatusCheck { get; set; }
        public string? Reason { get; set; }
        public List<int> WrongDocumentFieldIds { get; set; } = new();
        public string Description { get; set; } = String.Empty;
        public DateTime? CreatedDate { get; set; }
    }
}
