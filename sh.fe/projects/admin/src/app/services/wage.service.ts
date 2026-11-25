import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { IExportWage, IFindWage, IViewWagePaging } from '@/models/wage.model';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { apiUrl } from '@/shared/utils/api-url';

@Injectable({
    providedIn: 'root',
})
export class WageService {
    api = apiUrl('operation/project');
    http = inject(HttpClient);

    findPaging(projectId: number, query: IFindWage) {
        return this.http.get<IBaseResponseWithData<IViewWagePaging>>(`${this.api}/${projectId}/wage/paging`, { params: this.toParams(query) });
    }

    // Chỉ Admin / SuperAdmin; người khác BE trả JSON lỗi trong blob
    exportExcelUrl(projectId: number, query: IExportWage) {
        const params = new HttpParams({ fromObject: this.toParams(query) });
        return `${this.api}/${projectId}/wage/export-excel?${params.toString()}`;
    }

    // Bỏ tham số rỗng để BE không nhận chuỗi rỗng
    private toParams<T extends object>(query: T) {
        return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''));
    }
}
