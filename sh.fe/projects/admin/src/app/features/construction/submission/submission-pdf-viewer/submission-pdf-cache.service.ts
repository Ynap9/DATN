import { Injectable, OnDestroy } from '@angular/core';

// Giữ đúng một file PDF của màn đang mở để đổi Ngang / Dọc (dựng lại viewer) không tải lại file; khai ở providers của màn nên mất theo màn
@Injectable()
export class SubmissionPdfCacheService implements OnDestroy {
    private _fileUrl: string | null = null;
    private _objectUrl: string | null = null;

    find(fileUrl: string) {
        return this._fileUrl === fileUrl ? this._objectUrl : null;
    }

    // Thay mục cũ thì thu hồi objectUrl cũ
    save(fileUrl: string, file: Blob) {
        this.release();
        this._fileUrl = fileUrl;
        this._objectUrl = URL.createObjectURL(file);
        return this._objectUrl;
    }

    ngOnDestroy() {
        this.release();
    }

    private release() {
        if (this._objectUrl) {
            URL.revokeObjectURL(this._objectUrl);
        }
        this._fileUrl = null;
        this._objectUrl = null;
    }
}
