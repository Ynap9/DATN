namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class WageMemberDto
    {
        public int MemberId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string? FullName { get; set; }
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = String.Empty;
        public string Role { get; set; } = String.Empty;
        public string? ManagedByUserId { get; set; }
        public bool IsActive { get; set; }
        public bool IsRemoved { get; set; }
        public decimal? CompanyPrice { get; set; }
        public decimal? ActualPrice { get; set; }
    }
}
