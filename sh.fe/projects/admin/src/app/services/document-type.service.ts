import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { ICreateDocumentType, IDocumentTypeDropdown, IFindPagingDocumentType, IUpdateDocumentType, IViewDocumentType } from "@/models/document-type.model";
import { IViewDocumentField } from "@/models/document-field.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class DocumentTypeService {
    api = apiUrl('construction/document-type');
    http = inject(HttpClient);

    dropdown() {
        return this.http.get<IBaseResponseWithData<IDocumentTypeDropdown[]>>(`${this.api}/dropdown`);
    }

    findPaging(query: IFindPagingDocumentType) {
        return this.http.get<IBaseResponsePaging<IViewDocumentType>>(`${this.api}/paging`, { params: { ...query } });
    }

    findById(id: number) {
        return this.http.get<IBaseResponseWithData<IViewDocumentType>>(`${this.api}/${id}`);
    }

    getDocumentFields(id: number) {
        return this.http.get<IBaseResponseWithData<IViewDocumentField[]>>(`${this.api}/${id}/document-fields`);
    }

    excelTemplateUrl(id: number) {
        return `${this.api}/${id}/excel-template`;
    }

    create(body: ICreateDocumentType) {
        return this.http.post<IBaseResponse>(this.api, body);
    }

    update(id: number, body: IUpdateDocumentType) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body);
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }
}
