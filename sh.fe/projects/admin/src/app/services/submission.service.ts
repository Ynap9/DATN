import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    IFindPagingSubmission,
    IFindSubmissionFieldValue,
    ILatestWorkingSubmission,
    IListQaSubmission,
    IListSubmission,
    ISubmissionCount,
    ISubmissionFieldValue,
    ISubmissionFilter,
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

    // Bỏ tham số trống nhưng giữ số 0: statusCheck = 0 / statusQa = 0 là lọc thật
    private toParams(query: ISubmissionFilter) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
