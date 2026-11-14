import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
    IChangeOrderDocumentField,
    ICreateDocumentField,
    IDocumentFieldDropdown,
    IFieldImportHeader,
    IFindPagingDocumentField,
    IMapFieldImport,
    IQuickCreateDocumentField,
    IQuickDeleteDocumentField,
    ISheetHeader,
    IUpdateDocumentField,
    IUpdateSettingDocumentField,
    IViewDocumentField,
} from "@/models/document-field.model";
import { IBaseResponse, IBaseResponsePaging, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class DocumentFieldService {
    api = apiUrl('construction/document-field');
    http = inject(HttpClient);

    dropdown(documentTypeId: number) {
        return this.http.get<IBaseResponseWithData<IDocumentFieldDropdown[]>>(`${this.api}/dropdown/${documentTypeId}`);
    }

    findPaging(query: IFindPagingDocumentField) {
        return this.http.get<IBaseResponsePaging<IViewDocumentField>>(`${this.api}/paging`, { params: { ...query } });
    }

    getSheetHeaders(url: string) {
        return this.http.get<IBaseResponseWithData<ISheetHeader[]>>(`${this.api}/sheet-header`, { params: { url } });
    }

    mapFieldImport(query: IMapFieldImport) {
        return this.http.get<IBaseResponseWithData<IFieldImportHeader[]>>(`${this.api}/map-field-import`, { params: { ...query } });
    }

    findById(id: number) {
        return this.http.get<IBaseResponseWithData<IViewDocumentField>>(`${this.api}/${id}`);
    }

    create(body: ICreateDocumentField) {
        return this.http.post<IBaseResponse>(this.api, body);
    }

    quickCreate(body: IQuickCreateDocumentField) {
        return this.http.post<IBaseResponse>(`${this.api}/quick-create`, body);
    }

    update(id: number, body: IUpdateDocumentField) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}`, body);
    }

    updateSetting(id: number, body: IUpdateSettingDocumentField) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}/setting`, body);
    }

    changeOrderTableView(id: number, body: IChangeOrderDocumentField) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}/order-table-view`, body);
    }

    changeOrderListView(id: number, body: IChangeOrderDocumentField) {
        return this.http.put<IBaseResponse>(`${this.api}/${id}/order-list-view`, body);
    }

    quickDelete(body: IQuickDeleteDocumentField) {
        return this.http.delete<IBaseResponse>(`${this.api}/quick-delete`, { body });
    }

    delete(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }
}
