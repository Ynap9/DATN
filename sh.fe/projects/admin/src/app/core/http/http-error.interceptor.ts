import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { SKIP_ERROR_TOAST } from './http-context';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
    401: 'Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn',
    403: 'Bạn không có quyền thực hiện thao tác này',
    404: 'Đường dẫn không tồn tại',
};
const DEFAULT_ERROR_MESSAGE = 'Có lỗi xảy ra, vui lòng thử lại sau!';

export const httpErrorInterceptor: HttpInterceptorFn = (request, next) => {
    const messageService = inject(MessageService);

    return next(request).pipe(
        catchError((error: unknown) => {
            if (error instanceof HttpErrorResponse && !request.context.get(SKIP_ERROR_TOAST)) {
                messageService.add({ severity: 'error', detail: HTTP_ERROR_MESSAGES[error.status] ?? DEFAULT_ERROR_MESSAGE, life: 4000 });
            }
            return throwError(() => error);
        }),
    );
};
