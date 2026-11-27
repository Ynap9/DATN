import { Component, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { catchError, finalize, from, map, Observable, of, startWith, Subject, switchMap } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { SubmissionService } from '@/services/submission.service';
import { readErrorResponse } from '@/shared/utils/file-response';
import { SubmissionPdfCacheService } from './submission-pdf-cache.service';

// Lỗi HTTP (interceptor đã toast): khung chỉ báo chung để người dùng bấm Thử lại
const DEFAULT_ERROR_MESSAGE = 'Có lỗi xảy ra, vui lòng thử lại sau!';

// Khung PDF gốc của văn bản qua proxy-file, hiện bằng trình xem PDF của trình duyệt (43.6); file đã tải do SubmissionPdfCacheService của màn giữ
// nên viewer bị dựng lại (đổi Ngang / Dọc) cùng văn bản thì không tải lại
@Component({
    selector: 'app-submission-pdf-viewer',
    imports: [ButtonModule],
    templateUrl: './submission-pdf-viewer.component.html',
})
export class SubmissionPdfViewerComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sanitizer = inject(DomSanitizer);
    private readonly _pdfCache = inject(SubmissionPdfCacheService);
    private readonly _retry$ = new Subject<void>();

    fileUrl = input.required<string>();
    loading = signal(false);
    error = signal<string | null>(null);
    pdfUrl = signal<SafeResourceUrl | null>(null);

    constructor() {
        // Đổi văn bản → tải file mới; switchMap huỷ lượt tải của file cũ
        toObservable(this.fileUrl)
            .pipe(
                switchMap((fileUrl) => this._retry$.pipe(startWith(undefined), switchMap(() => this.download(fileUrl)))),
                takeUntilDestroyed(),
            )
            .subscribe(({ objectUrl, message }) => {
                if (message !== null) {
                    this.error.set(message);
                    return;
                }
                this.pdfUrl.set(this._sanitizer.bypassSecurityTrustResourceUrl(objectUrl!));
            });
    }

    retry() {
        this._retry$.next();
    }

    // Response JSON = lỗi BE (315 / 305, HTTP 200) → message (không lưu cache để Thử lại tải lại); còn lại là PDF
    private download(fileUrl: string): Observable<{ objectUrl?: string; message: string | null }> {
        this.pdfUrl.set(null);
        this.error.set(null);
        const cachedUrl = this._pdfCache.find(fileUrl);
        if (cachedUrl) {
            return of({ objectUrl: cachedUrl, message: null });
        }

        this.loading.set(true);
        return this._downloadService.get(this._submissionService.proxyFileUrl(fileUrl)).pipe(
            switchMap((response) =>
                from(readErrorResponse(response)).pipe(
                    map((error) => (error ? { message: error.message } : { objectUrl: this._pdfCache.save(fileUrl, response.body!), message: null })),
                ),
            ),
            catchError(() => of({ message: DEFAULT_ERROR_MESSAGE })),
            finalize(() => this.loading.set(false)),
        );
    }
}
