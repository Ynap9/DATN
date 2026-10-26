namespace sh.be.applications.Module.Construction.Dtos.QaAssignment
{
    public class FindQaTicketByIdDto : GetListQaTicketDto
    {
        public decimal? ThresholdPercent { get; set; }
        public string? StrategyCode { get; set; }
        public string? StrategyParamsJson { get; set; }
        public int? Seed { get; set; }
        public bool? StopEarlyOnReject { get; set; }
        public DateTime RoundStartedDate { get; set; }
        public List<GetListQaTicketSampleDto> Samples { get; set; } = new();
    }
}
