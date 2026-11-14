import { HttpResponse } from '@angular/common/http';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';

// API tải file của BE trả file khi thành công, lỗi nghiệp vụ vẫn là ApiResponse JSON nằm trong blob
export async function readErrorResponse(response: HttpResponse<Blob>): Promise<IBaseResponse | null> {
    if (!response.body || !response.headers.get('Content-Type')?.includes('application/json')) return null;
    return JSON.parse(await response.body.text());
}
