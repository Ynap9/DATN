namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class UpdateQaReviewerModeDto
    {
        public int ProjectId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string Mode { get; set; } = String.Empty;
    }
}
