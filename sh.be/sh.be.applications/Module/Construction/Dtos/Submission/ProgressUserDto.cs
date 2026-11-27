namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ProgressUserDto
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public bool IsRemoved { get; set; }
        public int WorkingDays { get; set; }
    }
}
