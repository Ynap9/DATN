import { IBaseRequestPaging } from '@/shared/models/request-paging.base.models';

export interface IFindPagingUser extends IBaseRequestPaging {
    sort?: string[];
    roleId?: number;
}

export interface ICreateUser {
    userName: string;
    fullName: string;
    email: string;
    phoneNumber?: string;
    password?: string;
    passwordConfirm?: string;
    isRandomPassword: boolean;
    roleId: number[];
}

export interface IUpdateUser {
    userName?: string;
    fullName?: string;
    email?: string;
    phoneNumber?: string;
}

export interface IUpdateRoleUser {
    userId: string;
    roleId: number[];
}

export interface IChangePassword {
    currentPassword: string;
    newPassword: string;
    newPasswordConfirm: string;
}

export interface IUserRolePaging {
    id: number;
    name: string;
}

export interface IViewUser {
    id: string;
    userName: string;
    fullName: string;
    email: string;
    roles: IUserRolePaging[];
    isLocked: boolean;
    createdBy?: string;
    createdDate?: string;
}

export interface IUserPermission {
    key: string;
    description: string;
    parentKey?: string;
    module: string;
}

export interface IUserRole {
    id: number;
    name: string;
    description: string;
    permissions: IUserPermission[];
}

export interface IViewMe {
    id: string;
    userName: string;
    fullName: string;
    email: string;
    roles: IUserRole[];
}
