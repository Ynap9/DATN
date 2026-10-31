import { AuthService } from '@/core/auth/service/auth.service';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '@env/environment';
import { catchError, switchMap, throwError } from 'rxjs';
import { SKIP_AUTH } from './http-context';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);

    if (req.context.get(SKIP_AUTH) || !req.url.startsWith(`${environment.baseUrl}/api/`)) {
        return next(req);
    }

    const credentialedReq = req.clone({ withCredentials: true });

    return next(credentialedReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status !== 401) {
                return throwError(() => error);
            }

            return authService.refreshAccessToken().pipe(
                catchError((refreshError) => {
                    authService.logout();
                    return throwError(() => refreshError);
                }),
                switchMap(() => next(credentialedReq)),
            );
        }),
    );
};
