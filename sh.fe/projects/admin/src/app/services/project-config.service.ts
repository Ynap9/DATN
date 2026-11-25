import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { IProjectConfig, ISaveProjectConfig } from "@/models/project-config.model";
import { IBaseResponse, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class ProjectConfigService {
    api = apiUrl('construction/project-config');
    http = inject(HttpClient);

    // Mọi dòng cấu hình, sắp theo Name; secret có giá trị trả "******"
    getList() {
        return this.http.get<IBaseResponseWithData<IProjectConfig[]>>(this.api);
    }

    create(body: ISaveProjectConfig) {
        return this.http.post<IBaseResponse>(this.api, body);
    }

    update(id: number, body: ISaveProjectConfig) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body);
    }

    // Link đăng nhập Google để cấp quyền Drive / Sheets; thiếu Client ID / secret / Redirect URI trả 304
    getGoogleAuthorizationUrl() {
        return this.http.get<IBaseResponseWithData<string>>(`${this.api}/google-authorization-url`);
    }
}
