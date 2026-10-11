import { Injectable } from '@angular/core';
import { TokenService } from './token.service';
import { BaseConsts } from '../../const/base.consts';

@Injectable({
    providedIn: 'root',
})
export class AppAuthService {
    constructor(
        private _tokenService: TokenService,
    ) { }

    connectAuthorize(): void {
        this._tokenService.clearAll();
        window.location.href = BaseConsts.redirectLoginPath;
    }
}
