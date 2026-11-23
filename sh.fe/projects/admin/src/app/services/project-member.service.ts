import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    IAddProjectLeader,
    IAddProjectMember,
    IAddProjectMemberResult,
    IFindMemberCandidate,
    IFindPagingProjectMember,
    IMemberCandidate,
    IRemoveProjectMemberResult,
    IUpdateMemberPrices,
    IUpdateMemberPricesResult,
    IUpdateProjectMember,
    IViewProjectMember,
} from "@/models/project-member.model";
import { IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class ProjectMemberService {
    api = apiUrl('operation/project');
    http = inject(HttpClient);

    findPaging(projectId: number, query: IFindPagingProjectMember) {
        const params = Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
        return this.http.get<IBaseResponsePaging<IViewProjectMember>>(`${this.api}/${projectId}/member/paging`, { params });
    }

    findCandidates(projectId: number, query: IFindMemberCandidate) {
        const params = Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
        return this.http.get<IBaseResponsePaging<IMemberCandidate>>(`${this.api}/${projectId}/member/candidates`, { params });
    }

    addLeaders(projectId: number, body: IAddProjectLeader) {
        return this.http.post<IBaseResponseWithData<IAddProjectMemberResult>>(`${this.api}/${projectId}/leader`, body);
    }

    addMembers(projectId: number, body: IAddProjectMember) {
        return this.http.post<IBaseResponseWithData<IAddProjectMemberResult>>(`${this.api}/${projectId}/member`, body);
    }

    updatePrices(projectId: number, body: IUpdateMemberPrices) {
        return this.http.put<IBaseResponseWithData<IUpdateMemberPricesResult>>(`${this.api}/${projectId}/member/price`, body);
    }

    update(projectId: number, memberId: number, body: IUpdateProjectMember) {
        return this.http.put<IBaseResponseWithData<IViewProjectMember>>(`${this.api}/${projectId}/member/${memberId}`, body);
    }

    remove(projectId: number, memberId: number) {
        return this.http.delete<IBaseResponseWithData<IRemoveProjectMemberResult>>(`${this.api}/${projectId}/member/${memberId}`);
    }
}
