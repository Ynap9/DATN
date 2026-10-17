using sh.be.applications.Module.Construction.Dtos.Submission;

namespace sh.be.applications.Module.Construction.Interfaces.Services
{
    public interface ISubmissionImportService
    {
        Task<ViewVerifyImportAppendSubmissionDto> VerifyImportAppend(ImportSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportAppend(ImportSubmissionDto dto);
        Task<ViewVerifyImportBlankSubmissionDto> VerifyImportBlank(ImportSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportBlank(ImportSubmissionDto dto);
        Task<ViewVerifyImportOverwriteSubmissionDto> VerifyImportOverwrite(ImportOverwriteSubmissionDto dto);
        Task<ViewImportSubmissionResultDto> ImportOverwrite(ImportOverwriteSubmissionDto dto);
    }
}
