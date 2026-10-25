using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface ISubmissionQuery
    {
        Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto);
        Task<ViewSubmissionDetailDto?> FindById(int id, ISubmissionFilter filter, bool includePosition);
        Task<int?> FindNextId(int id, ISubmissionFilter filter);
        Task<int?> FindPreviousId(int id, ISubmissionFilter filter);
        Task<ViewCountSubmissionDto> Count(ISubmissionFilter filter);
        Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int submissionId, FindPagingSubmissionLogDto dto);
        Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto);
        Task<ViewQaSubmissionDetailDto?> FindQaById(int id, ISubmissionFilter filter, bool includePosition);
        Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int submissionId, FindPagingSubmissionQaLogDto dto);
        Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto);
        Task<ViewSubmissionTreeDto> FindTree(ISubmissionFilter filter);
        Task<BaseResponsePagingDto<GetListProjectSubmissionLogDto>> FindProjectLogPaging(FindPagingProjectSubmissionLogDto dto);
        Task<ViewSubmissionSummaryDto> FindSummary(int projectId, int documentTypeId);
        Task<ViewQaStatsSummaryDto> FindQaStatsSummary(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive);
        Task<List<GetListQaFieldStatsDto>> FindQaFieldStats(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive);
        Task<BaseResponsePagingDto<GetListQaEntryUserStatsDto>> FindQaEntryUserStatsPaging(FindPagingQaEntryUserStatsDto dto, DateTime fromDate,
            DateTime toDateExclusive);
        Task<List<GetListSubmissionTimelineDto>> FindTimeline(int submissionId);
        Task<SubmissionExportTableDto> FindExportTable(ISubmissionFilter filter);
        Task<SubmissionExportNameDto?> FindExportName(int projectId, int documentTypeId);
    }
}
