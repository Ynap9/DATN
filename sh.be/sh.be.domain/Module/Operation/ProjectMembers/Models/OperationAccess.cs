using sh.be.domain.Module.Construction.DocumentTypes.Entities;
using sh.be.domain.Module.Construction.Projects.Entities;

namespace sh.be.domain.Module.Operation.ProjectMembers.Models
{
    public record OperationAccess(Project Project, DocumentType DocumentType, bool IsAdmin, string? LeaderUserId);
}
