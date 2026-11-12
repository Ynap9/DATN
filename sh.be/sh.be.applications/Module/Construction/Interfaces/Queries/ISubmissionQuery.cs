using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Queries
{
    public interface ISubmissionQuery
    {
        Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto, string? restrictedUserId = null);
        Task<ViewSubmissionDetailDto?> FindById(int id, ISubmissionFilter filter, bool includePosition, string? restrictedUserId = null);
        Task<int?> FindNextId(int id, ISubmissionFilter filter, string? restrictedUserId = null);
        Task<int?> FindPreviousId(int id, ISubmissionFilter filter, string? restrictedUserId = null);
        Task<ViewCountSubmissionDto> Count(ISubmissionFilter filter, string? restrictedUserId = null);
        Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int submissionId, FindPagingSubmissionLogDto dto);
        Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto, string? restrictedUserId = null);
        Task<ViewQaSubmissionDetailDto?> FindQaById(int id, ISubmissionFilter filter, bool includePosition, string? restrictedUserId = null);
        Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int submissionId, FindPagingSubmissionQaLogDto dto);
        Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto, string? restrictedUserId = null);
        Task<ViewSubmissionTreeDto> FindTree(ISubmissionFilter filter, string? restrictedUserId = null);
        Task<BaseResponsePagingDto<GetListProjectSubmissionLogDto>> FindProjectLogPaging(FindPagingProjectSubmissionLogDto dto);
        Task<ViewSubmissionSummaryDto> FindSummary(int projectId, int documentTypeId);
        Task<ViewQaStatsSummaryDto> FindQaStatsSummary(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive);
        Task<List<GetListQaFieldStatsDto>> FindQaFieldStats(FindQaStatsDto dto, DateTime fromDate, DateTime toDateExclusive);
        Task<BaseResponsePagingDto<GetListQaEntryUserStatsDto>> FindQaEntryUserStatsPaging(FindPagingQaEntryUserStatsDto dto, DateTime fromDate,
            DateTime toDateExclusive);
        Task<List<GetListSubmissionTimelineDto>> FindTimeline(int submissionId);
        Task<SubmissionExportTableDto> FindExportTable(ExportSubmissionDto filter);
        Task<SubmissionExportNameDto?> FindExportName(int projectId, int documentTypeId);
    }
}
