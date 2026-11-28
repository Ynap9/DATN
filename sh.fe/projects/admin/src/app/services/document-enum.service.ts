import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ISaveDocumentEnum, IViewDocumentEnum } from '@/models/document-enum.model';
import { IBaseResponse, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { apiUrl } from '@/shared/utils/api-url';

@Injectable({
    providedIn: 'root',
})
export class DocumentEnumService {
    api = apiUrl('construction/document-type');
    http = inject(HttpClient);

    getList(documentTypeId: number) {
        return this.http.get<IBaseResponseWithData<IViewDocumentEnum[]>>(`${this.api}/${documentTypeId}/enum`);
    }

    // Trả Id danh mục vừa tạo
    create(documentTypeId: number, body: ISaveDocumentEnum) {
        return this.http.post<IBaseResponseWithData<number>>(`${this.api}/${documentTypeId}/enum`, body);
    }

    update(documentTypeId: number, enumId: number, body: ISaveDocumentEnum) {
        return this.http.put<IBaseResponse>(`${this.api}/${documentTypeId}/enum/${enumId}`, body);
    }

    delete(documentTypeId: number, enumId: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${documentTypeId}/enum/${enumId}`);
    }
}
