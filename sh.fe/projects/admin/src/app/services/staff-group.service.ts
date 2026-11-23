import { HttpClient, HttpContext } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { SKIP_ERROR_TOAST } from "@/core/http/http-context";
import {
    IChangeStaffGroupMembers,
    ICreateStaffGroup,
    IFindPagingStaffGroup,
    IFindStaffGroupCandidate,
    IStaffGroupCandidate,
    IStaffGroupDetail,
    IStaffGroupOwner,
    IUpdateStaffGroup,
    IViewStaffGroup,
} from "@/models/staff-group.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

// API ghi trả HTTP 400 khi sai định dạng DTO: tắt toast câu chung để dialog tự báo "Dữ liệu gửi lên không hợp lệ"
const WRITE_CONTEXT = () => new HttpContext().set(SKIP_ERROR_TOAST, true);

@Injectable({
  providedIn: 'root'
})
export class StaffGroupService {
    api = apiUrl('operation/staff-group');
    http = inject(HttpClient);

    findPaging(query: IFindPagingStaffGroup) {
        const params = this.toParams(query);
        return this.http.get<IBaseResponsePaging<IViewStaffGroup>>(`${this.api}/paging`, { params });
    }

    findById(id: number) {
        return this.http.get<IBaseResponseWithData<IStaffGroupDetail>>(`${this.api}/${id}`);
    }

    create(body: ICreateStaffGroup) {
        return this.http.post<IBaseResponseWithData<IStaffGroupDetail>>(this.api, body, { context: WRITE_CONTEXT() });
    }

    update(id: number, body: IUpdateStaffGroup) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body, { context: WRITE_CONTEXT() });
    }

    remove(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    addMembers(id: number, body: IChangeStaffGroupMembers) {
        return this.http.post<IBaseResponseWithData<IStaffGroupDetail>>(`${this.api}/${id}/members`, body, { context: WRITE_CONTEXT() });
    }

    removeMembers(id: number, body: IChangeStaffGroupMembers) {
        return this.http.delete<IBaseResponseWithData<IStaffGroupDetail>>(`${this.api}/${id}/members`, { body, context: WRITE_CONTEXT() });
    }

    candidates(query: IFindStaffGroupCandidate) {
        const params = this.toParams(query);
        return this.http.get<IBaseResponsePaging<IStaffGroupCandidate>>(`${this.api}/candidates`, { params });
    }

    owners() {
        return this.http.get<IBaseResponseWithData<IStaffGroupOwner[]>>(`${this.api}/owners`);
    }

    // Bỏ tham số rỗng để BE không nhận chuỗi rỗng
    private toParams<T extends object>(query: T) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
