namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class ViewWageSummaryDto
    {
        public int DataQuantity { get; set; }
        public int QaQuantity { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal? CompanyAmount { get; set; }
    }
}
