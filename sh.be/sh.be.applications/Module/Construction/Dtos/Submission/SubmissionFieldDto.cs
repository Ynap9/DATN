namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SubmissionFieldDto
    {
        public int DocumentFieldId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Field { get; set; } = String.Empty;
        public string FieldType { get; set; } = String.Empty;
        public bool IsShow { get; set; }
        public bool IsChargeable { get; set; }
        public int OrderListView { get; set; }
        public bool IsShowListView { get; set; }
        public int? Width { get; set; }
        public int? SubmissionDataId { get; set; }
        public string Data { get; set; } = String.Empty;
        public string? IndexGoogleSheet { get; set; }
    }
}
