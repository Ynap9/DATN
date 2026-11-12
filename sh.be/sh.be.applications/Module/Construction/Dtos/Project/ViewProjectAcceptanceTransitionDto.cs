namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class ViewProjectAcceptanceTransitionDto
    {
        public string Action { get; set; } = String.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int? OldStatus { get; set; }
        public int? NewStatus { get; set; }
        public bool IsForced { get; set; }
        public bool IsAutomatic { get; set; }
        public string? Reason { get; set; }
    }
}
