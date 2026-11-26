import { Component, DestroyRef, inject, input, model, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { finalize } from 'rxjs';
import { IDeleteSubmissionResult } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SUBMISSION_ERROR_CODE } from '@/shared/constants/submission.constants';
import { SubmissionRow } from '../submission-columns';

// BE ghi các Id lỗi ở cuối câu: "... Id = 12, 15"
const ERROR_IDS_PATTERN = /Id = ([\d,\s]+)$/;

const ERROR_CHIPS: Record<number, { label: string; severity: 'info' | 'success' }> = {
    [SUBMISSION_ERROR_CODE.IN_PROGRESS_NOT_DELETABLE]: { label: 'Đang sửa / QA', severity: 'info' },
    [SUBMISSION_ERROR_CODE.CHECKED_NOT_DELETABLE]: { label: 'Đã kiểm', severity: 'success' },
};

const CONSEQUENCES = [
    'Dữ liệu đã nhập của các văn bản này bị xoá. Lịch sử kiểm tra và QA vẫn giữ.',
    'Văn bản được gỡ khỏi phiếu nhập liệu của người đang giữ, tiền công không còn tính cho văn bản này.',
    'File PDF vẫn nằm trên kho lưu trữ. Import lại cùng tên file sẽ tạo văn bản mới.',
];

interface IDeleteError {
    rows: SubmissionRow[];
    chip: { label: string; severity: 'info' | 'success' };
}

// Hỏi xác nhận xoá các văn bản đã chọn; BE từ chối 325 / 336 thì đổi sang danh sách văn bản lỗi để người dùng bỏ chọn chúng
@Component({
    selector: 'app-submission-delete-dialog',
    imports: [ButtonModule, DialogModule, MessageModule, TagModule],
    templateUrl: './submission-delete-dialog.component.html',
})
export class SubmissionDeleteDialogComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);

    rows = input.required<SubmissionRow[]>();
    projectId = input.required<number>();
    documentTypeId = input.required<number>();
    visible = model(false);

    deleted = output<IDeleteSubmissionResult>();
    // Mã lỗi khác 325 / 336 (đã toast), màn cha quyết định có cần tải lại dự án không
    failed = output<number>();
    // Id các văn bản lỗi mà người dùng chọn bỏ khỏi danh sách đã chọn
    unselect = output<number[]>();

    readonly consequences = CONSEQUENCES;
    error: IDeleteError | null = null;

    close() {
        this.visible.set(false);
    }

    confirm() {
        this.loading = true;
        this._submissionService
            .delete({ projectId: this.projectId(), documentTypeId: this.documentTypeId(), ids: this.rows().map((row) => row.id) })
            .pipe(finalize(() => (this.loading = false)), takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.deleted.emit(res.data);
                    return;
                }
                const ids = ERROR_IDS_PATTERN.exec(res.message ?? '')?.[1].split(',').map(Number) ?? [];
                const errorRows = this.rows().filter((row) => ids.includes(row.id));
                const chip = ERROR_CHIPS[res.code];
                if (chip && errorRows.length) {
                    this.error = { rows: errorRows, chip };
                    return;
                }
                this.messageError(res.message);
                this.failed.emit(res.code);
            });
    }

    unselectFailed() {
        this.unselect.emit(this.error!.rows.map((row) => row.id));
        this.close();
    }
}
