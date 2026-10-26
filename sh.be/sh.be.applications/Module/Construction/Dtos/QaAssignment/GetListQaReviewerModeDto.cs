namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class GetListQaReviewerModeDto
    {
        public string UserId { get; set; } = String.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string Mode { get; set; } = String.Empty;
        public DateTime? ModifiedDate { get; set; }
    }
}
