import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingRole extends IBaseRequestPaging {}

export interface IViewRole {
    id: number;
    name: string;
    description: string;
}

export interface IRoleDropdown {
    id: number;
    name: string;
}

export interface ICreateRole {
    id: number;
    name: string;
    description: string;
}

export interface IUpdateRole {
    name: string;
    description: string;
}

export interface IRolePermissionTree {
    key: string;
    parentKey?: string;
    description: string;
    module: string;
    isChecked: boolean;
    children: IRolePermissionTree[];
}

export interface IUpdateRolePermissionNode {
    key: string;
    isChecked: boolean;
    children: IUpdateRolePermissionNode[];
}

export interface IUpdateRolePermission {
    roleId: number;
    permissions: IUpdateRolePermissionNode[];
}
