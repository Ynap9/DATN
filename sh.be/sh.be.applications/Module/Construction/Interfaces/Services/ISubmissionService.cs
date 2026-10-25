using sh.be.applications.Base.Dtos.DataReader;
using sh.be.applications.Module.Construction.Dtos.Submission;
using sh.be.shared.HttpRequest.BaseRequest;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface ISubmissionService
    {
        Task<ViewVerifyImportAppendSubmissionDto> VerifyImportAppend(ImportSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportAppend(ImportSubmissionDto dto);
        Task<ViewVerifyImportBlankSubmissionDto> VerifyImportBlank(ImportSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportBlank(ImportSubmissionDto dto);
        Task<ViewVerifyImportOverwriteSubmissionDto> VerifyImportOverwrite(ImportOverwriteSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportOverwrite(ImportOverwriteSubmissionDto dto);
        Task<BaseResponsePagingDto<GetListSubmissionDto>> FindPaging(FindPagingSubmissionDto dto);
        Task<ViewSubmissionDetailDto> FindById(int id, FindSubmissionDetailDto dto);
        Task<ViewSubmissionDetailDto> FindNext(int id, SubmissionFilterDto dto);
        Task<ViewSubmissionDetailDto> FindPrevious(int id, SubmissionFilterDto dto);
        Task<ViewCountSubmissionDto> Count(SubmissionFilterDto dto);
        Task<ViewCheckSubmissionResultDto> Check(CheckSubmissionDto dto);
        Task<BaseResponsePagingDto<GetListSubmissionLogDto>> FindLogPaging(int id, FindPagingSubmissionLogDto dto);
        Task<BaseResponsePagingDto<GetListQaSubmissionDto>> FindQaPaging(FindPagingQaSubmissionDto dto);
        Task<ViewQaSubmissionDetailDto> FindQaById(int id, FindQaSubmissionDetailDto dto);
        Task<ViewQaResultDto> ReviewQa(ReviewQaSubmissionDto dto);
        Task<BaseResponsePagingDto<GetListSubmissionQaLogDto>> FindQaLogPaging(int id, FindPagingSubmissionQaLogDto dto);
        Task<List<GetListSubmissionFieldValueDto>> FindFieldValues(FindSubmissionFieldValueDto dto);
        Task<ViewSubmissionTreeDto> FindTree(FindSubmissionTreeDto dto);
        Task<BaseResponsePagingDto<GetListProjectSubmissionLogDto>> FindProjectLogPaging(FindPagingProjectSubmissionLogDto dto);
        Task<ViewSubmissionSummaryDto> FindSummary(FindSubmissionSummaryDto dto);
        Task<ViewQaStatsSummaryDto> FindQaStatsSummary(FindQaStatsDto dto);
        Task<List<GetListQaFieldStatsDto>> FindQaFieldStats(FindQaStatsDto dto);
        Task<BaseResponsePagingDto<GetListQaEntryUserStatsDto>> FindQaEntryUserStatsPaging(FindPagingQaEntryUserStatsDto dto);
        Task<List<GetListSubmissionTimelineDto>> FindTimeline(int id);
        Task<ViewDeleteSubmissionResultDto> Delete(DeleteSubmissionDto dto);
        Task<ExportFileDto> ExportExcel(ExportSubmissionDto dto);
        Task<ViewSubmissionExportJobDto> CreateExportJob(ExportSubmissionDto dto);
        Task<ViewSubmissionExportJobDto> FindExportJob(string jobId);
    }
}
