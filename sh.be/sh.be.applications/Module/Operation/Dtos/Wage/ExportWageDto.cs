namespace sh.be.applications.Module.Operation.Dtos.Wage
{
    public class ExportWageDto
    {
        public int? DocumentTypeId { get; set; }
        public string? Role { get; set; }
        private string? _keyword;
        public string? Keyword
        {
            get => _keyword;
            set => _keyword = value?.Trim();
        }
    }
}
