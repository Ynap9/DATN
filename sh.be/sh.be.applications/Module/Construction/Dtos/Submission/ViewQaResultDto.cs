namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewQaResultDto
    {
        public int SubmissionId { get; set; }
        public int StatusCheck { get; set; }
        public int StatusQa { get; set; }
        public string? QaReason { get; set; }
        public List<int> WrongDocumentFieldIds { get; set; } = new();
        public int QaLogId { get; set; }
    }
}
