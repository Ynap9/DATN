namespace sh.be.applications.Module.Operation.Dtos.Assignment
{
    public class AssignmentPreviewDto
    {
        public string Role { get; set; } = String.Empty;
        public int TotalDocuments { get; set; }
        public int NewDocuments { get; set; }
        public int AlreadyHeldDocuments { get; set; }
        public int ConflictDocuments { get; set; }
        public int LaterDocuments { get; set; }
        public int SameAsEntryDocuments { get; set; }
        public int ReturnedToSameDocuments { get; set; }
        public List<AssignmentConflictDto> Conflicts { get; set; } = new();
        public List<AssignmentItemDto> NormalizedItems { get; set; } = new();
        public List<AssignmentItemDto> EmptyItems { get; set; } = new();
        public bool CanSave { get; set; }
        public List<string> BlockReasons { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
