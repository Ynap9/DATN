import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { IFindPagingStaffGroup, IViewStaffGroup } from "@/models/staff-group.model";
import { IBaseResponsePaging } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class StaffGroupService {
    api = apiUrl('operation/staff-group');
    http = inject(HttpClient);

    findPaging(query: IFindPagingStaffGroup) {
        const params = Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
        return this.http.get<IBaseResponsePaging<IViewStaffGroup>>(`${this.api}/paging`, { params });
    }
}
