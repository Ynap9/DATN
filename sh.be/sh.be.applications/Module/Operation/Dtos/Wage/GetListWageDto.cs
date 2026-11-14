namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class GetListWageDto
    {
        public int MemberId { get; set; }
        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string? FullName { get; set; }
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = String.Empty;
        public string Role { get; set; } = String.Empty;
        public bool IsActive { get; set; }
        public bool IsRemoved { get; set; }
        public int Quantity { get; set; }
        public int DocumentCount { get; set; }
        public int ReviewCount { get; set; }
        public int ReviewDocumentCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public decimal? CompanyPrice { get; set; }
        public decimal? ActualPrice { get; set; }
        public bool IsPriceMissing { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal? CompanyAmount { get; set; }
    }
}
