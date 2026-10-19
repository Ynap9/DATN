using sh.be.applications.Module.Construction.Dtos.DocumentField;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionDataDto
    {
        public int Id { get; set; }
        public int DocumentFieldId { get; set; }
        public string Data { get; set; } = String.Empty;
        public int? IndexRow { get; set; }
        public string? IndexGoogleSheet { get; set; }
        public GetListDocumentFieldDto DocumentField { get; set; } = new();
    }
}
