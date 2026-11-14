namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class WageQuantityDto
    {
        public int DocumentCount { get; set; }
        public int FieldCount { get; set; }
        public int ReviewCount { get; set; }
        public int ReviewDocumentCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
