import { HttpClient, HttpContext } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { SKIP_ERROR_TOAST } from "@/core/http/http-context";
import {
    IAssignmentChangeResult,
    IAssignmentNode,
    IAssignmentPreview,
    IAssignmentRequest,
    IAssignmentTicket,
    IFindAssignmentProfiles,
    IFindAssignmentTicket,
    IFindAssignmentTree,
    IRevokeAssignment,
    IRevokeQaOwnership,
    ITransferAssignment,
    ITransferQaOwnership,
} from "@/models/assignment.model";
import { IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

// Thu hồi / chuyển trả HTTP 400 khi sai định dạng DTO: tắt toast câu chung để dialog tự báo "Dữ liệu gửi lên không hợp lệ"
const CHANGE_CONTEXT = () => new HttpContext().set(SKIP_ERROR_TOAST, true);

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
    api = apiUrl('operation/project');
    http = inject(HttpClient);

    findTree(projectId: number, documentTypeId: number, query: IFindAssignmentTree) {
        const params = this.toParams(query);
        return this.http.get<IBaseResponsePaging<IAssignmentNode>>(`${this.pairApi(projectId, documentTypeId)}/assignment-tree`, { params });
    }

    // box "" (hộp rỗng tên) vẫn phải gửi lên, nên không bỏ tham số box
    findProfiles(projectId: number, documentTypeId: number, query: IFindAssignmentProfiles) {
        const { box, ...rest } = query;
        const params = { ...this.toParams(rest), box };
        return this.http.get<IBaseResponsePaging<IAssignmentNode>>(`${this.pairApi(projectId, documentTypeId)}/assignment-tree/profiles`, { params });
    }

    preview(projectId: number, documentTypeId: number, body: IAssignmentRequest) {
        return this.http.post<IBaseResponseWithData<IAssignmentPreview>>(`${this.pairApi(projectId, documentTypeId)}/assignment/preview`, body);
    }

    assign(projectId: number, documentTypeId: number, body: IAssignmentRequest) {
        return this.http.post<IBaseResponseWithData<IAssignmentTicket>>(`${this.pairApi(projectId, documentTypeId)}/assignment`, body);
    }

    findTicketPaging(projectId: number, documentTypeId: number, query: IFindAssignmentTicket) {
        const params = this.toParams(query);
        return this.http.get<IBaseResponsePaging<IAssignmentTicket>>(`${this.pairApi(projectId, documentTypeId)}/assignment/ticket/paging`, { params });
    }

    findTicket(projectId: number, documentTypeId: number, ticketId: number) {
        return this.http.get<IBaseResponseWithData<IAssignmentTicket>>(`${this.pairApi(projectId, documentTypeId)}/assignment/${ticketId}`);
    }

    revoke(projectId: number, documentTypeId: number, body: IRevokeAssignment) {
        return this.http.post<IBaseResponseWithData<IAssignmentChangeResult>>(`${this.pairApi(projectId, documentTypeId)}/assignment/revoke`, body, { context: CHANGE_CONTEXT() });
    }

    transfer(projectId: number, documentTypeId: number, body: ITransferAssignment) {
        return this.http.post<IBaseResponseWithData<IAssignmentChangeResult>>(`${this.pairApi(projectId, documentTypeId)}/assignment/transfer`, body, { context: CHANGE_CONTEXT() });
    }

    revokeQa(projectId: number, documentTypeId: number, body: IRevokeQaOwnership) {
        return this.http.post<IBaseResponseWithData<IAssignmentChangeResult>>(`${this.pairApi(projectId, documentTypeId)}/assignment/qa/revoke`, body, { context: CHANGE_CONTEXT() });
    }

    transferQa(projectId: number, documentTypeId: number, body: ITransferQaOwnership) {
        return this.http.post<IBaseResponseWithData<IAssignmentChangeResult>>(`${this.pairApi(projectId, documentTypeId)}/assignment/qa/transfer`, body, { context: CHANGE_CONTEXT() });
    }

    private pairApi(projectId: number, documentTypeId: number) {
        return `${this.api}/${projectId}/document-type/${documentTypeId}`;
    }

    // Bỏ tham số rỗng để BE không nhận chuỗi rỗng
    private toParams<T extends object>(query: T) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
