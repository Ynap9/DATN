import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ICreateRole, IFindPagingRole, IRoleDropdown, IRolePermissionTree, IUpdateRole, IUpdateRolePermission, IViewRole } from "@/models/role.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class RoleService {
    api = apiUrl('auth/role');
    http = inject(HttpClient);

    dropdown() {
        return this.http.get<IBaseResponseWithData<IRoleDropdown[]>>(`${this.api}/dropdown`);
    }

    findPaging(query: IFindPagingRole) {
        return this.http.get<IBaseResponsePaging<IViewRole>>(`${this.api}/paging`, { params: { ...query } });
    }

    getPermissionTree(id: number) {
        return this.http.get<IBaseResponseWithData<IRolePermissionTree[]>>(`${this.api}/${id}/tree`);
    }

    updatePermission(body: IUpdateRolePermission) {
        return this.http.put<IBaseResponse>(`${this.api}/permission`, body);
    }

    create(body: ICreateRole) {
        return this.http.post<IBaseResponse>(this.api, body);
    }

    update(id: number, body: IUpdateRole) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }
}
