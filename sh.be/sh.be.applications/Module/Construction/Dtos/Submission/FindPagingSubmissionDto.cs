using Microsoft.AspNetCore.Mvc;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Dtos.Submission
{
    public class FindPagingSubmissionDto : BaseRequestPagingDto, ISubmissionFilter
    {
        [FromQuery(Name = "projectId")]
        public int ProjectId { get; set; }

        [FromQuery(Name = "documentTypeId")]
        public int DocumentTypeId { get; set; }

        [FromQuery(Name = "statusCheck")]
        public int? StatusCheck { get; set; }

        [FromQuery(Name = "documentFieldId1")]
        public int? DocumentFieldId1 { get; set; }

        [FromQuery(Name = "fieldValue1")]
        public string? FieldValue1 { get; set; }

        [FromQuery(Name = "documentFieldId2")]
        public int? DocumentFieldId2 { get; set; }

        [FromQuery(Name = "fieldValue2")]
        public string? FieldValue2 { get; set; }
    }
}
