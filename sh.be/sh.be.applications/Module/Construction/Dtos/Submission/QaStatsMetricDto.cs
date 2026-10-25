namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class QaStatsMetricDto
    {
        public int ReviewCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int ReviewedDocumentCount { get; set; }
        public int FirstReviewCount { get; set; }
        public int FirstPassCount { get; set; }
        public decimal FirstPassRate { get; set; }
        public int WrongFieldCount { get; set; }
        public int ReviewedCellCount { get; set; }
        public decimal FieldAccuracyRate { get; set; }
        public decimal AvgReturnPerDocument { get; set; }
    }
}
