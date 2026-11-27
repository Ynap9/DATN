import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ICreateProject, IFindPagingProject, IReopenAcceptance, ITransitionAcceptance, IUpdateProject, IViewProject, IViewProjectAcceptance, IViewProjectPair } from "@/models/project.model";
import {
    IDeadlineCheckStatus,
    IFindProjectDeadline,
    IFindProjectDeadlineNotification,
    IProjectDeadline,
    IProjectDeadlineNotification,
    IRunDeadlineCheck,
} from "@/models/project-deadline.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
    api = apiUrl('construction/project');
    http = inject(HttpClient);

    findPaging(query: IFindPagingProject) {
        const params = Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
        return this.http.get<IBaseResponsePaging<IViewProjectPair>>(`${this.api}/paging`, { params });
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

    findDeadlinePaging(query: IFindProjectDeadline) {
        return this.http.get<IBaseResponsePaging<IProjectDeadline>>(`${this.api}/deadline`, { params: this.toParams(query) });
    }

    runDeadlineCheck() {
        return this.http.post<IBaseResponseWithData<IRunDeadlineCheck>>(`${this.api}/deadline-check/run`, null);
    }

    findDeadlineCheckStatus() {
        return this.http.get<IBaseResponseWithData<IDeadlineCheckStatus>>(`${this.api}/deadline-check/status`);
    }

    findDeadlineNotificationPaging(query: IFindProjectDeadlineNotification) {
        return this.http.get<IBaseResponsePaging<IProjectDeadlineNotification>>(`${this.api}/deadline-notification/paging`, { params: this.toParams(query) });
    }

    // Bỏ tham số rỗng nhưng giữ số 0 và false (isSuccess = false là lọc "Lỗi")
    private toParams(query: object) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
