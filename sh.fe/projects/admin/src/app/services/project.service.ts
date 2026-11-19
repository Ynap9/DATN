import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ICreateProject, IFindPagingProject, IReopenAcceptance, ITransitionAcceptance, IUpdateProject, IViewProject, IViewProjectAcceptance, IViewProjectPair } from "@/models/project.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
    api = apiUrl('construction/project');
    http = inject(HttpClient);

    findPaging(query: IFindPagingProject) {
        return this.http.get<IBaseResponsePaging<IViewProjectPair>>(`${this.api}/paging`, { params: { ...query } });
    }

    findById(id: number) {
        return this.http.get<IBaseResponseWithData<IViewProject>>(`${this.api}/${id}`);
    }

    create(body: ICreateProject) {
        return this.http.post<IBaseResponseWithData<IViewProject>>(this.api, body);
    }

    update(id: number, body: IUpdateProject) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body);
    }

    deleteCascade(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/admin/delete/${id}`);
    }

    getAcceptance(id: number) {
        return this.http.get<IBaseResponseWithData<IViewProjectAcceptance>>(`${this.api}/${id}/acceptance`);
    }

    startAcceptance(id: number, body: ITransitionAcceptance) {
        return this.http.post<IBaseResponseWithData<IViewProjectAcceptance>>(`${this.api}/${id}/acceptance/start`, body);
    }

    finishAcceptance(id: number, body: ITransitionAcceptance) {
        return this.http.post<IBaseResponseWithData<IViewProjectAcceptance>>(`${this.api}/${id}/acceptance/finish`, body);
    }

    reopenAcceptance(id: number, body: IReopenAcceptance) {
        return this.http.post<IBaseResponseWithData<IViewProjectAcceptance>>(`${this.api}/${id}/acceptance/reopen`, body);
    }

    acceptanceReportUrl(id: number) {
        return `${this.api}/${id}/acceptance/report`;
    }
}
