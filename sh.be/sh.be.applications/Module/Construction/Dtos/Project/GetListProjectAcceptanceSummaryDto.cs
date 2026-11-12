namespace sh.be.applications.Module.Construction.Dtos.Project
{
    public class GetListProjectAcceptanceSummaryDto
    {
        public int? DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public bool IsRemoved { get; set; }
        public int TotalSubmissions { get; set; }
        public int TotalNotChecked { get; set; }
        public int TotalChecked { get; set; }
        public int TotalReturned { get; set; }
        public int TotalQaNotReviewed { get; set; }
        public int TotalQaPassed { get; set; }
        public int TotalQaFailed { get; set; }
        public decimal QaPassedPercent { get; set; }
    }
}
