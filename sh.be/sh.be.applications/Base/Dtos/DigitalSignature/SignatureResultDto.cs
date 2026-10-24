namespace sh.be.applications.Base.Dtos.DigitalSignature
{
    public class SignatureResultDto
    {
        public string RequestId { get; set; } = String.Empty;
        public string? SignatureBase64 { get; set; }
        public string? Error { get; set; }
        public bool IsSessionLost { get; set; }
    }
}
