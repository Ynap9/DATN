import { Injectable } from '@angular/core';
import {
    ActivatedRouteSnapshot,
    CanActivate,
    CanActivateChild,
    Router,
    RouterStateSnapshot,
    UrlTree
} from '@angular/router';

import { TokenService } from '../../lib-components/src/my-lib-components/shared/services/auth/token.service';
import { BaseConsts } from '../../lib-components/src/my-lib-components/shared/const/base.consts';

@Injectable({
    providedIn: 'root',
})
export class AuthGuard implements CanActivate, CanActivateChild {

    static readonly HOME_PATH = '/admin';

    constructor(
        private tokenService: TokenService,
        private router: Router
    ) { }

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean | UrlTree {

        const loginPath = BaseConsts.redirectLoginPath;
        const token = this.tokenService.getToken();

        if (!token) {
            if (state.url.includes(loginPath)) {
                return true;
            }

            return this.router.createUrlTree([loginPath]);
        }

        if (state.url.includes(loginPath)) {
            return this.router.createUrlTree([AuthGuard.HOME_PATH]);
        }

        return true;
    }

    canActivateChild(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): boolean | UrlTree {
        return this.canActivate(route, state);
    }
}
