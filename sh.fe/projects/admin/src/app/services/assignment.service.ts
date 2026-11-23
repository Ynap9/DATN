import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    IAssignmentNode,
    IAssignmentPreview,
    IAssignmentRequest,
    IAssignmentTicket,
    IFindAssignmentProfiles,
    IFindAssignmentTree,
} from "@/models/assignment.model";
import { IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

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

    private pairApi(projectId: number, documentTypeId: number) {
        return `${this.api}/${projectId}/document-type/${documentTypeId}`;
    }

    // Bỏ tham số rỗng để BE không nhận chuỗi rỗng
    private toParams<T extends object>(query: T) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
