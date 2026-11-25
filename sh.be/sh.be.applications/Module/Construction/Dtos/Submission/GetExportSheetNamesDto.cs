using Microsoft.AspNetCore.Mvc;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class GetExportSheetNamesDto
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }

        [FromQuery(Name = "url")]
        public string Url { get; set; } = String.Empty;
    }
}
