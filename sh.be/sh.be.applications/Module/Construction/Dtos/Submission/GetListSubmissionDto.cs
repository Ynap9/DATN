namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetListSubmissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
        public string FileUrlS3Object { get; set; } = String.Empty;
        public string ProfileRange { get; set; } = String.Empty;
        public string Box { get; set; } = String.Empty;
        public int StatusCheck { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public List<SubmissionCellDto> Cells { get; set; } = new();
    }
}
