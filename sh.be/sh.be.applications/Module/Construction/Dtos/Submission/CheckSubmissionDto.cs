namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class CheckSubmissionDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public List<CheckSubmissionDataDto> Datas { get; set; } = new();
        public string? Note { get; set; }
    }
}
