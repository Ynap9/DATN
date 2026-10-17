namespace sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch
{
    public class FindSigningBatchByIdDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string? SignedFolderName { get; set; }
        public string? CertificateThumbprint { get; set; }
        public int Status { get; set; }
        public int TotalFiles { get; set; }
        public int SignedFiles { get; set; }
        public int FailedFiles { get; set; }
        public string? StopReason { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
