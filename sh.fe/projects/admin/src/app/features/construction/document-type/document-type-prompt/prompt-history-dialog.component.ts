import { formatDate } from '@angular/common';
import { Component, DestroyRef, inject, LOCALE_ID, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { IPromptTemplateVersion } from '@/models/prompt-template.model';
import { PromptTemplateService } from '@/services/prompt-template.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { getPromptTemplateStatusBadge, PROMPT_TEMPLATE_STATUS } from '@/shared/constants/prompt-template.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { PROMPT_RESTORE_ACTION, PromptRestoreCellComponent, PromptVersionRow } from './prompt-restore-cell.component';

export interface IPromptHistoryDialogData {
    documentTypeId: number;
    canRestore: boolean;
    // Form đang có thay đổi chưa lưu: khôi phục sẽ tải lại form nên phải báo trước
    isDirty: boolean;
}

type HistoryRow = PromptVersionRow & { versionLabel: string; publishedLabel: string };

// Danh sách phiên bản; khôi phục thành nháp xong đóng với true để tab Prompt tải lại
@Component({
    selector: 'app-prompt-history-dialog',
    imports: [DataTable],
    templateUrl: './prompt-history-dialog.component.html',
})
export class PromptHistoryDialogComponent extends BaseComponent implements OnInit {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _promptTemplateService = inject(PromptTemplateService);
    private readonly _locale = inject(LOCALE_ID);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _data: IPromptHistoryDialogData = inject(DynamicDialogConfig).data;

    readonly columns: IColumn<HistoryRow>[] = [
        { header: 'Phiên bản', field: 'versionLabel', headerContainerStyle: 'width: 96px' },
        { header: 'Trạng thái', cellViewType: CellViewTypes.BADGE, badge: (row) => getPromptTemplateStatusBadge(row.status), headerContainerStyle: 'width: 120px' },
        { header: 'Ghi chú', field: 'note' },
        { header: 'Xuất bản', field: 'publishedLabel' },
        ...(this._data.canRestore ? [{ header: 'Thao tác', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: PromptRestoreCellComponent }] : []),
    ];

    rows: HistoryRow[] = [];

    override ngOnInit(): void {
        this.loading = true;
        this._promptTemplateService
            .findVersions(this._data.documentTypeId)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    const hasDraft = res.data.some((version) => version.status === PROMPT_TEMPLATE_STATUS.DRAFT);
                    this.rows = res.data.map((version) => ({
                        ...version,
                        hasDraft,
                        versionLabel: `v${version.version}`,
                        publishedLabel: this.toPublishedLabel(version),
                    }));
                }
            });
    }

    onAction(event: ITableEvent<HistoryRow>) {
        if (event.type === PROMPT_RESTORE_ACTION && event.row) {
            this.confirmRestore(event.row);
        }
    }

    private confirmRestore(row: HistoryRow) {
        const lostChanges = this._data.isDirty ? ' Thay đổi chưa lưu trên form sẽ mất.' : '';
        this.confirmAction({ header: 'Khôi phục thành nháp', message: `Tạo bản nháp mới từ ${row.versionLabel}?${lostChanges}` }, () => {
            this._promptTemplateService
                .restore(row.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, `Đã khôi phục ${row.versionLabel} thành nháp`)) {
                        this._ref.close(true);
                    }
                });
        });
    }

    private toPublishedLabel(version: IPromptTemplateVersion): string {
        if (!version.publishedDate) {
            return '';
        }
        return `${version.publishedByName || '—'} · ${formatDate(version.publishedDate, 'dd/MM/yyyy HH:mm', this._locale)}`;
    }
}
