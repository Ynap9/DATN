namespace sh.be.applications.Module.Operation.Dtos.StaffGroup
{
    public class ViewStaffGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string OwnerUserId { get; set; } = String.Empty;
        public string? OwnerFullName { get; set; }
        public string? Role { get; set; }
        public int MemberCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
