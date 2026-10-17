namespace sh.be.applications.Module.DigitalSignature.Dtos.SigningBatch
{
    public class CreateSigningBatchDto
    {
        public int TemplateId { get; set; }
        public string? SignedFolderName { get; set; }
    }
}
