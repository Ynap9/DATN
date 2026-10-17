namespace sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch
{
    public class GetListSigningBatchFileDto
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string FileName { get; set; } = String.Empty;
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SignedAt { get; set; }
        public DateTime? TimestampedAt { get; set; }
        public bool? IsSignatureValid { get; set; }
        public string? SignatureValidationMessage { get; set; }
    }
}
