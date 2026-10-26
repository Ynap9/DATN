namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public interface ISubmissionFilter
    {
        int ProjectId { get; }
        int DocumentTypeId { get; }
        int? StatusCheck { get; }
        int? StatusQa { get; }
        int? DocumentFieldId1 { get; }
        string? FieldValue1 { get; }
        int? DocumentFieldId2 { get; }
        string? FieldValue2 { get; }
        string? Keyword { get; }
        int? QaTicketId { get; }
    }
}
