namespace sh.be.domain.Module.Operation.ProjectMembers.Models
{
    public record RevokeOnRemoveResult(int RevokedDocumentCount, int KeptDocumentCount, List<int> CancelledAssignmentTicketIds, List<int> CancelledQaTicketIds);
}
