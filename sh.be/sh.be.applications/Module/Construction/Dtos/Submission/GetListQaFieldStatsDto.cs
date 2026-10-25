namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListQaFieldStatsDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public bool IsConfigured { get; set; }
        public bool IsChargeable { get; set; }
        public int ReviewCount { get; set; }
        public int WrongCount { get; set; }
        public decimal WrongRate { get; set; }
        public decimal AccuracyRate { get; set; }
    }
}
