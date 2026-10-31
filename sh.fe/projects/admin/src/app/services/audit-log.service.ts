import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { IAuditLogDetail, IFindPagingAuditLog, IViewAuditLog } from "@/models/audit-log.model";
import { IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
    api = apiUrl('core/audit-log');
    http = inject(HttpClient);

    findPaging(query: IFindPagingAuditLog) {
        return this.http.get<IBaseResponsePaging<IViewAuditLog>>(`${this.api}/paging`, { params: { ...query } });
    }

    findById(id: number) {
        return this.http.get<IBaseResponseWithData<IAuditLogDetail>>(`${this.api}/${id}`);
    }
}
