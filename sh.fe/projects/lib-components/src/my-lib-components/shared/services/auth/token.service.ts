import { Injectable } from '@angular/core';
import { BaseConsts } from '../../const/base.consts';

@Injectable({
    providedIn: 'root'
})
export class TokenService {
    constructor() { }

    getToken(): string {
        return localStorage.getItem(BaseConsts.authorization.accessToken) || '';
    }

    getRefreshToken(): string {
        return localStorage.getItem(BaseConsts.authorization.refreshToken) || '';
    }

    clearToken() {
        localStorage.removeItem(BaseConsts.authorization.refreshToken);
        localStorage.removeItem(BaseConsts.authorization.accessToken);
    }

    setToken(accessToken: string) {
        localStorage.setItem(BaseConsts.authorization.accessToken, accessToken);
    }

    setRefreshToken(refreshToken: string) {
        localStorage.setItem(BaseConsts.authorization.refreshToken, refreshToken);
    }

    clearAll() {
        this.clearToken();
    }
}
