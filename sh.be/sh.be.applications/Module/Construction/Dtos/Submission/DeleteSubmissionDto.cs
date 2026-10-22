namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class DeleteSubmissionDto
    {
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public List<int> Ids { get; set; } = new();
    }
}
