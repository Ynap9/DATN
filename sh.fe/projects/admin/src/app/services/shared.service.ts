import { IViewMe } from '@/models/user.model';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class SharedService {
    private _permissions: string[] = []
    private _roles: string[] = []
    private _currentUser: IViewMe | null = null

    get currentUser(): IViewMe | null {
        return this._currentUser;
    }

    get permissions(): string[] {
        return this._permissions;
    }

    get roles(): string[] {
        return this._roles;
    }

    public isGranted(permission: string) {
        return this._roles.includes(AuthConstants.SUPER_ADMIN_ROLE) || this._permissions.includes(permission);
    }

    public setPermissions(data: string[]) {
        this._permissions = data
    }

    public isSuperAdmin(): boolean{
        return this._roles.includes(AuthConstants.SUPER_ADMIN_ROLE);
    } 

    public setRoles(data: string[]) {
        this._roles = data
    }

    public setCurrentUser(data: IViewMe | null) {
        this._currentUser = data
    }

    public clearAll() {
        this.setCurrentUser(null)
        this.setRoles([])
        this.setPermissions([])
    }
}
