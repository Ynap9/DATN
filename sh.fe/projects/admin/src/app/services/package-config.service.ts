import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { IFindPagingPackageSchemaField, IPackageTypeDropdown, IViewPackageSchemaField } from '@/models/package-config.model';
import { IBaseResponsePaging, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { apiUrl } from '@/shared/utils/api-url';

@Injectable({
    providedIn: 'root',
})
export class PackageConfigService {
    api = apiUrl('packaging/config');
    http = inject(HttpClient);

    dropdown() {
        return this.http.get<IBaseResponseWithData<IPackageTypeDropdown[]>>(`${this.api}/dropdown`);
    }

    findPaging(query: IFindPagingPackageSchemaField) {
        return this.http.get<IBaseResponsePaging<IViewPackageSchemaField>>(`${this.api}/paging`, { params: { ...query } });
    }

    // metadataTypes lặp lại: ?metadataTypes=1&metadataTypes=2
    excelTemplateUrl(packageType: number, metadataTypes: number[]) {
        const params = new HttpParams({ fromObject: { packageType, metadataTypes } });
        return `${this.api}/excel-template?${params.toString()}`;
    }
}
