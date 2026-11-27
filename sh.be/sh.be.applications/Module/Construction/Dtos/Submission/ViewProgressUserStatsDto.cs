namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewProgressUserStatsDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsQaEnabled { get; set; }
        public List<GetListProgressDataUserDto> DataUsers { get; set; } = new();
        public List<GetListProgressQaUserDto> QaUsers { get; set; } = new();
    }
}
