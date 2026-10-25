namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class ViewQaSamplingConfigDto
    {
        public int ProjectId { get; set; }
        public int SampleSize { get; set; }
        public decimal ThresholdPercent { get; set; }
        public string StrategyCode { get; set; } = String.Empty;
        public string? StrategyParamsJson { get; set; }
        public bool StopEarlyOnReject { get; set; }
        public bool IsDefault { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
