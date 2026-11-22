import { HttpResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { catchError, finalize, from, map, Observable, of, startWith, Subject, switchMap } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { SubmissionService } from '@/services/submission.service';
import { readErrorResponse } from '@/shared/utils/file-response';

// Lỗi HTTP (interceptor đã toast): khung chỉ báo chung để người dùng bấm Thử lại
const DEFAULT_ERROR_MESSAGE = 'Có lỗi xảy ra, vui lòng thử lại sau!';

// Khung PDF gốc của văn bản qua proxy-file, hiện bằng trình xem PDF của trình duyệt (43.6)
@Component({
    selector: 'app-submission-pdf-viewer',
    imports: [ButtonModule],
    templateUrl: './submission-pdf-viewer.component.html',
})
export class SubmissionPdfViewerComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sanitizer = inject(DomSanitizer);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _retry$ = new Subject<void>();
    private _objectUrl: string | null = null;

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
            .subscribe(({ response, message }) => {
                if (message !== null) {
                    this.error.set(message);
                    return;
                }
                this._objectUrl = URL.createObjectURL(response!.body!);
                this.pdfUrl.set(this._sanitizer.bypassSecurityTrustResourceUrl(this._objectUrl));
            });
        this._destroyRef.onDestroy(() => this.releasePdf());
    }

    retry() {
        this._retry$.next();
    }

    // Response JSON = lỗi BE (315 / 305, HTTP 200) → message; còn lại là PDF
    private download(fileUrl: string): Observable<{ response?: HttpResponse<Blob>; message: string | null }> {
        this.releasePdf();
        this.error.set(null);
        this.loading.set(true);
        return this._downloadService.get(this._submissionService.proxyFileUrl(fileUrl)).pipe(
            switchMap((response) => from(readErrorResponse(response)).pipe(map((error) => ({ response, message: error ? error.message : null })))),
            catchError(() => of({ message: DEFAULT_ERROR_MESSAGE })),
            finalize(() => this.loading.set(false)),
        );
    }

    private releasePdf() {
        if (this._objectUrl) {
            URL.revokeObjectURL(this._objectUrl);
            this._objectUrl = null;
        }
        this.pdfUrl.set(null);
    }
}
