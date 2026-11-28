namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class OutOfEnumFieldDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public int Count { get; set; }
        public List<string> Samples { get; set; } = new();
    }
}
