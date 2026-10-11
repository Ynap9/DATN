import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpParams, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { TokenService } from './token.service';
import { AppAuthService } from './app-auth.service';
import { Utils } from '../../../utils';
import { catchError, EMPTY, finalize, Observable } from 'rxjs';
import { ErrorCode } from '../../const/error-code.const';

@Injectable()
export class HandleInterceptorService implements HttpInterceptor {
    constructor(
        private messageService: MessageService,
        private _tokenService: TokenService,
        private _authService: AppAuthService,
    ) { }

    requestPostApi: string;

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        let token: string = this._tokenService.getToken();
        const method: string = request.method;

        const methodCreateOrUpdates = ['POST', 'PUT', 'PATCH'];
        const paramRemoves = ['unAuthorize', 'enableMultileRequest', 'skipErrorToast'];
        const unAuthorize = request.params.get('unAuthorize') === true.toString();
        const enableMultileRequest = !!request.params.get('enableMultileRequest');
        const skipErrorToast = request.params.get('skipErrorToast') === true.toString();

        if (methodCreateOrUpdates.includes(method) && !enableMultileRequest) {
            if (request.url !== this.requestPostApi) {
                this.requestPostApi = request.url;
            } else {
                return EMPTY;
            }
        }

        let params = new HttpParams();
        let paramKeys = request.params.keys();
        paramKeys.forEach(key => {
            if (!paramRemoves.includes(key)) {
                request.params.getAll(key).forEach(value => {
                    params = params.append(key, value);
                })
            }
        })
        if (token) {
            let headers = !unAuthorize ? { 'Authorization': `Bearer ${token}` } : {};
            request = request.clone({
                setHeaders: headers,
                params: params,
            });
        }

        return next.handle(request).pipe(
            catchError(err => {
                if (err instanceof HttpErrorResponse) {
                    if (err?.status === 401 && token) {
                        this._authService.connectAuthorize();
                    } else {
                        let message: string = '';
                        let statusCode: number = err?.status;
                        switch (statusCode) {
                            case 404:
                                message = 'Đường dẫn không tồn tại';
                                break;
                            case 401:
                                message = 'Tài khoản không có quyền truy cập';
                                break;
                            case 403:
                                message = err?.error?.message || err?.message;
                                break;
                            default:
                                message = 'Có lỗi xảy ra vui lòng thử lại sau!';
                                break;
                        }
                        Utils.log(`statusCode ${err?.status}: ${ErrorCode.list[err?.status] || ''}`, `${message || err?.error?.message || err?.message}`)
                        if (statusCode !== 200 && message && !skipErrorToast) {
                            this.messageService.add({
                                severity: 'error',
                                detail: message,
                                life: 2000
                            });
                        }
                    }
                }
                throw (err);
            }),
            finalize(() => {
                this.requestPostApi = '';
            }),
        );
    }
}
