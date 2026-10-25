namespace sh.be.applications.Module.Construction.Dtos.ProjectDeadline
{
    public class GetListProjectDeadlineNotificationDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = String.Empty;
        public int NotificationType { get; set; }
        public DateTime EndedAt { get; set; }
        public int DaysLeft { get; set; }
        public string ToAddress { get; set; } = String.Empty;
        public string Subject { get; set; } = String.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
