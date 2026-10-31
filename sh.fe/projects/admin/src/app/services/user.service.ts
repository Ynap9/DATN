import { SKIP_ERROR_TOAST } from "@/core/http/http-context";
import { HttpClient, HttpContext } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { IChangePassword, ICreateUser, IFindPagingUser, IUpdateRoleUser, IUpdateUser, IViewMe, IViewUser } from "@/models/user.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class UserService {
    api = apiUrl('auth/user');
    http = inject(HttpClient);

    getMe() {
        const context = new HttpContext().set(SKIP_ERROR_TOAST, true);
        return this.http.get<IBaseResponseWithData<IViewMe>>(`${this.api}/me`, { context });
    }

    findPaging(query: IFindPagingUser) {
        return this.http.get<IBaseResponsePaging<IViewUser>>(`${this.api}/paging`, { params: { ...query } });
    }

    create(body: ICreateUser) {
        return this.http.post<IBaseResponse>(this.api, body);
    }

    update(body: IUpdateUser) {
        return this.http.put<IBaseResponse>(this.api, body);
    }

    delete(userId: string) {
        return this.http.delete<IBaseResponse>(`${this.api}/${userId}`);
    }

    changePassword(body: IChangePassword) {
        return this.http.put<IBaseResponse>(`${this.api}/change-password`, body);
    }

    updateRole(body: IUpdateRoleUser) {
        return this.http.put<IBaseResponse>(`${this.api}/role`, body);
    }
}
