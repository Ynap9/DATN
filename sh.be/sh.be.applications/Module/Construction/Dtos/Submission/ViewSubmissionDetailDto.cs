namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ViewSubmissionDetailDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public string FileUrlS3Object { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
        public int StatusCheck { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int? Position { get; set; }
        public int? TotalItems { get; set; }
        public List<SubmissionFieldDto> Fields { get; set; } = new();
    }
}
