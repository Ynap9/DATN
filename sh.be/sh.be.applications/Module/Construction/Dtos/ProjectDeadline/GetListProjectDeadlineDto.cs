namespace sh.be.applications.Module.Construction.Dtos.ProjectDeadline
{
    public class GetListProjectDeadlineDto
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = String.Empty;
        public int Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DaysLeft { get; set; }
        public int DeadlineType { get; set; }
        public DateTime? LastNotifiedDate { get; set; }
    }
}
