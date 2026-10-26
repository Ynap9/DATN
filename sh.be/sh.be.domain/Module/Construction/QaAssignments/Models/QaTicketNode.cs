namespace sh.be.domain.Module.Construction.QaAssignments.Models
{
    public record QaTicketNode(string Box, string? ProfileRange)
    {
        public bool Contains(QaCandidate candidate)
        {
            return candidate.Box == Box && (ProfileRange == null || candidate.ProfileRange == ProfileRange);
        }
    }
}
