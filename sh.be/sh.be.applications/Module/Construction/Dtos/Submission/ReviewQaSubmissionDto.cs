namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ReviewQaSubmissionDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public int Result { get; set; }
        public string? Reason { get; set; }
        public List<int> WrongDocumentFieldIds { get; set; } = new();
    }
}
