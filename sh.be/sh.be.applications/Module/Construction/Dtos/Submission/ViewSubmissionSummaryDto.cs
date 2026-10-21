namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewSubmissionSummaryDto
    {
        public int TotalSubmissions { get; set; }
        public int TotalNotChecked { get; set; }
        public int TotalChecked { get; set; }
        public int TotalReturned { get; set; }
        public decimal CheckedPercent { get; set; }
        public int TotalQaNotReviewed { get; set; }
        public int TotalQaPassed { get; set; }
        public int TotalQaFailed { get; set; }
        public int TotalFields { get; set; }
        public int TotalChargeableFields { get; set; }
        public int TotalFilledCells { get; set; }
        public int TotalChargeableFilledCellsOfChecked { get; set; }
    }
}
