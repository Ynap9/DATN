import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    ICheckSubmission,
    ICheckSubmissionResult,
    IFindPagingQaEntryUserStats,
    IFindPagingSubmission,
    IFindStats,
    IFindSubmissionDetail,
    IFindSubmissionFieldValue,
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
    ISubmissionFieldValue,
    ISubmissionFilter,
    ISubmissionQaLog,
    ISubmissionTimeline,
} from "@/models/submission.model";
import { LatestWorkingType } from "@/shared/constants/submission.constants";
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

    // Bỏ tham số trống nhưng giữ số 0: statusCheck = 0 / statusQa = 0 là lọc thật
    private toParams(query: ISubmissionFilter | IFindStats) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
