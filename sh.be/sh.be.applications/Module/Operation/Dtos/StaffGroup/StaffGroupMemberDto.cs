namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class StaffGroupMemberDto
    {
        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string FullName { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string RoleNames { get; set; } = String.Empty;
        public DateTime? AddedDate { get; set; }
    }
}
