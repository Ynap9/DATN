namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionColumnDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public int OrderTableView { get; set; }
        public int? Width { get; set; }
    }
}
