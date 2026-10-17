namespace sh.be.applications.Base.Dtos.Signing
{
    public class SignatureRequestDto
    {
        public string RequestId { get; set; } = String.Empty;
        public string DataBase64 { get; set; } = String.Empty;
    }
}
