import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '@env/environment';

export const baseUrlInterceptor: HttpInterceptorFn = (request, next) => {
    const isBackendUrl = /^\/(api|connect)\//.test(request.url);
    return next(isBackendUrl ? request.clone({ url: `${environment.baseUrl}${request.url}` }) : request);
};
