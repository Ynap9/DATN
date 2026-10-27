namespace sh.be.applications.Module.Operation.Dtos.ProjectMember
{
    public class ViewProjectMemberDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = String.Empty;
        public string? ManagedByUserId { get; set; }
        public string? ManagedByFullName { get; set; }
        public int? SourceStaffGroupId { get; set; }
        public string? SourceStaffGroupName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int HeldTicketCount { get; set; }
    }
}
