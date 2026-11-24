using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class ExportSheetSubmissionDto : ExportSubmissionDto
    {
        [FromQuery(Name = "url")]
        public string Url { get; set; } = String.Empty;

        [FromQuery(Name = "sheetName")]
        public string SheetName { get; set; } = String.Empty;

        [FromQuery(Name = "startRow")]
        public int StartRow { get; set; }
    }
}
