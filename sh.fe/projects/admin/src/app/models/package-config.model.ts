import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IPackageSchemaDropdown {
    packageSchemaId: number;
    packageType: number;
    metadataType: number;
    name: string;
}

export interface IPackageTypeDropdown {
    packageType: number;
    code: string;
    name: string;
    schemas: IPackageSchemaDropdown[];
}

export interface IFindPagingPackageSchemaField extends IBaseRequestPaging {
    packageType: number;
    metadataType?: number;
}

export interface IPackageCodeValue {
    code: string;
    name: string;
}

export interface IViewPackageSchemaField {
    id: number;
    packageSchemaId: number;
    packageType: number;
    metadataType: number;
    schemaName: string;
    elementName: string;
    displayName: string;
    dataType: number;
    maxLength: number | null;
    requirement: number;
    conditionElement: string | null;
    conditionValues: string | null;
    codeGroup: string | null;
    allowMultipleValues: boolean;
    description: string | null;
    sortOrder: number;
    codes: IPackageCodeValue[];
}
