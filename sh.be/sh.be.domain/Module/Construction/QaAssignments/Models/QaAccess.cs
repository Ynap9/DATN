namespace sh.be.domain.Module.Construction.QaAssignments.Models
{
    public record QaAccess(bool IsAdmin, IReadOnlySet<(int ProjectId, int DocumentTypeId)> ManagedPairs);
}
