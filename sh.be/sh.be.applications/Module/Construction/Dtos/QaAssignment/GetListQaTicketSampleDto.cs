namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class GetListQaTicketSampleDto
    {
        public int SubmissionId { get; set; }
        public string FileName { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public int? FirstResult { get; set; }
        public int StatusCheck { get; set; }
        public int StatusQa { get; set; }
    }
}
