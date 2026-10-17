namespace sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch
{
    public class SigningBatchProgressDto
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public int TotalFiles { get; set; }
        public int SignedFiles { get; set; }
        public int FailedFiles { get; set; }
        public string? StopReason { get; set; }
        public bool IsRunning { get; set; }
    }
}
