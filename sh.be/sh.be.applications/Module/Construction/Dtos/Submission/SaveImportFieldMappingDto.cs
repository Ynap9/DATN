namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class SaveImportFieldMappingDto
    {
        public int ProjectId { get; set; }
        public int DocumentTypeId { get; set; }
        public List<string> Headers { get; set; } = new();
        public int StartColumn { get; set; } = 1;
        public List<SaveImportFieldMappingItemDto> Fields { get; set; } = new();
    }
}
