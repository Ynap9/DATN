namespace sh.be.applications.Module.DigitalSignature.Dtos.Template
{
    public class GetListTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string? StampImageUrl { get; set; }
        public string? WetSignatureImageUrl { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
