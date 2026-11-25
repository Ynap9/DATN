import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    ICheckSubmission,
    ICheckSubmissionResult,
    IExportSheetNamesQuery,
    IExportSheetSubmission,
    IExportSubmission,
    IFindPagingQaEntryUserStats,
    IFindPagingSubmission,
    IFindStats,
    IFindSubmissionDetail,
    IFindSubmissionFieldValue,
    IImportExcelOverwriteSubmission,
    IImportExcelSubmission,
    IImportOverwriteSubmission,
    IImportSubmission,
    IImportSubmissionResult,
    ILatestWorkingSubmission,
    IListQaSubmission,
    IListSubmission,
    IProgressDailyStats,
    IQaEntryUserStats,
    IQaFieldStats,
    IQaStatsSummary,
    IQaSubmissionDetail,
    IReviewQaResult,
    IReviewQaSubmission,
    ISubmissionCount,
    ISubmissionDetail,
    ISubmissionExportJob,
    ISubmissionExportSheetJob,
    ISubmissionFieldValue,
    ISubmissionFilter,
    ISubmissionImportJob,
    ISubmissionQaLog,
    ISubmissionTimeline,
    IUploadImportExcelResult,
    IVerifyImportResultMap,
} from "@/models/submission.model";
import { ImportKind, LatestWorkingType } from "@/shared/constants/submission.constants";
import { IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class SubmissionService {
    api = apiUrl('construction/submission');
    http = inject(HttpClient);

    paging(query: IFindPagingSubmission) {
        return this.http.get<IBaseResponsePaging<IListSubmission>>(`${this.api}/paging`, { params: this.toParams(query) });
    }

    count(query: ISubmissionFilter) {
        return this.http.get<IBaseResponseWithData<ISubmissionCount>>(`${this.api}/count`, { params: this.toParams(query) });
    }

    qaPaging(query: IFindPagingSubmission) {
        return this.http.get<IBaseResponsePaging<IListQaSubmission>>(`${this.api}/qa/paging`, { params: this.toParams(query) });
    }

    fieldValues(query: IFindSubmissionFieldValue) {
        return this.http.get<IBaseResponseWithData<ISubmissionFieldValue[]>>(`${this.api}/field-values`, { params: this.toParams(query) });
    }

    latestWorking(projectId: number, documentTypeId: number, type: LatestWorkingType) {
        return this.http.get<IBaseResponseWithData<ILatestWorkingSubmission | null>>(`${this.api}/latest-working`, { params: { projectId, documentTypeId, type } });
    }

    findById(id: number, query: IFindSubmissionDetail) {
        return this.http.get<IBaseResponseWithData<ISubmissionDetail>>(`${this.api}/${id}`, { params: this.toParams(query) });
    }

    // Văn bản kế / trước trong bộ lọc; hết văn bản → 317
    next(id: number, query: ISubmissionFilter) {
        return this.http.get<IBaseResponseWithData<ISubmissionDetail>>(`${this.api}/${id}/next`, { params: this.toParams(query) });
    }

    previous(id: number, query: ISubmissionFilter) {
        return this.http.get<IBaseResponseWithData<ISubmissionDetail>>(`${this.api}/${id}/previous`, { params: this.toParams(query) });
    }

    check(body: ICheckSubmission) {
        return this.http.put<IBaseResponseWithData<ICheckSubmissionResult>>(`${this.api}/check`, body);
    }

    findQaById(id: number, query: IFindSubmissionDetail) {
        return this.http.get<IBaseResponseWithData<IQaSubmissionDetail>>(`${this.api}/${id}/qa`, { params: this.toParams(query) });
    }

    // pageSize = -1: lấy hết lượt QA của văn bản
    qaLogs(id: number) {
        return this.http.get<IBaseResponsePaging<ISubmissionQaLog>>(`${this.api}/${id}/qa-logs`, { params: { pageNumber: 1, pageSize: -1 } });
    }

    reviewQa(body: IReviewQaSubmission) {
        return this.http.put<IBaseResponseWithData<IReviewQaResult>>(`${this.api}/qa`, body);
    }

    timeline(id: number) {
        return this.http.get<IBaseResponseWithData<ISubmissionTimeline[]>>(`${this.api}/${id}/timeline`);
    }

    // Tải bằng DownloadService: thành công là PDF, lỗi là JSON IBaseResponse (HTTP 200)
    proxyFileUrl(fileUrl: string) {
        return `${this.api}/proxy-file?url=${encodeURIComponent(fileUrl)}`;
    }

    progressDailyStats(query: IFindStats) {
        return this.http.get<IBaseResponseWithData<IProgressDailyStats>>(`${this.api}/progress-stats/daily`, { params: this.toParams(query) });
    }

    qaStatsSummary(query: IFindStats) {
        return this.http.get<IBaseResponseWithData<IQaStatsSummary>>(`${this.api}/qa-stats/summary`, { params: this.toParams(query) });
    }

    qaStatsFields(query: IFindStats) {
        return this.http.get<IBaseResponseWithData<IQaFieldStats[]>>(`${this.api}/qa-stats/fields`, { params: this.toParams(query) });
    }

    qaStatsEntryUsers(query: IFindPagingQaEntryUserStats) {
        return this.http.get<IBaseResponsePaging<IQaEntryUserStats>>(`${this.api}/qa-stats/entry-users`, { params: this.toParams(query) });
    }

    // Chạy thử import từ Google Sheet của dự án, không ghi DB
    verifyImport<K extends ImportKind>(kind: K, body: IImportSubmission | IImportOverwriteSubmission) {
        return this.http.post<IBaseResponseWithData<IVerifyImportResultMap[K]>>(`${this.api}/import/${kind}/verify`, body);
    }

    // Import đồng bộ từ Google Sheet, có thể mất tới 1 phút
    importSheet(kind: ImportKind, body: IImportSubmission | IImportOverwriteSubmission) {
        return this.http.post<IBaseResponseWithData<IImportSubmissionResult>>(`${this.api}/import/${kind}`, body);
    }

    // observe 'events' + reportProgress để hiện % đang tải lên
    uploadImportExcel(file: File) {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<IBaseResponseWithData<IUploadImportExcelResult>>(`${this.api}/import/excel/upload`, formData, {
            reportProgress: true,
            observe: 'events',
        });
    }

    verifyImportExcel<K extends ImportKind>(kind: K, body: IImportExcelSubmission | IImportExcelOverwriteSubmission) {
        return this.http.post<IBaseResponseWithData<IVerifyImportResultMap[K]>>(`${this.api}/import/excel/${kind}/verify`, body);
    }

    // Đưa import Excel vào hàng chờ chạy nền, trả jobId để hỏi trạng thái
    createImportExcelJob(kind: ImportKind, body: IImportExcelSubmission | IImportExcelOverwriteSubmission) {
        return this.http.post<IBaseResponseWithData<ISubmissionImportJob>>(`${this.api}/import/excel/${kind}`, body);
    }

    findImportJob(jobId: string) {
        return this.http.get<IBaseResponseWithData<ISubmissionImportJob>>(`${this.api}/import/excel/job/${jobId}`);
    }

    // BE đọc bộ lọc + columns từ query (FromQuery), không có body
    createExportJob(query: IExportSubmission) {
        return this.http.post<IBaseResponseWithData<ISubmissionExportJob>>(`${this.api}/export-excel/job`, null, { params: this.toParams(query) });
    }

    // Gọi lại khi link tải hết hạn (15 phút) để lấy downloadUrl mới
    findExportJob(jobId: string) {
        return this.http.get<IBaseResponseWithData<ISubmissionExportJob>>(`${this.api}/export-excel/job/${jobId}`);
    }

    // Tên các tab của Google Sheet sẽ xuất vào, BE đọc bằng tài khoản Gmail đã kết nối
    getExportSheetNames(query: IExportSheetNamesQuery) {
        return this.http.get<IBaseResponseWithData<string[]>>(`${this.api}/export-sheet/sheet-names`, { params: this.toParams(query) });
    }

    // Như createExportJob, thêm url / sheetName / startRow; bộ lọc + columns đọc từ query, không có body
    createExportSheetJob(query: IExportSheetSubmission) {
        return this.http.post<IBaseResponseWithData<ISubmissionExportSheetJob>>(`${this.api}/export-sheet/job`, null, { params: this.toParams(query) });
    }

    findExportSheetJob(jobId: string) {
        return this.http.get<IBaseResponseWithData<ISubmissionExportSheetJob>>(`${this.api}/export-sheet/job/${jobId}`);
    }

    // Bỏ tham số trống nhưng giữ số 0: statusCheck = 0 / statusQa = 0 là lọc thật; mảng giữ nguyên để gửi lặp
    private toParams(query: ISubmissionFilter | IFindStats | IExportSubmission | IExportSheetNamesQuery) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
