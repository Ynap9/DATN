import { Component, inject } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PROMPT_NOTE_MAX_LENGTH } from '@/shared/constants/prompt-template.constants';
import { SharedImports } from '@/shared/import.shared';

export interface IPromptPublishDialogData {
    // Không có = chưa có nháp, số phiên bản chỉ biết sau khi lưu
    version?: number;
    publishedVersion?: number;
    note: string;
}

// Xác nhận xuất bản; đóng với ghi chú (chuỗi, có thể rỗng) khi đồng ý, null khi huỷ
@Component({
    selector: 'app-prompt-publish-dialog',
    imports: [SharedImports, ShInputTextComponent],
    templateUrl: './prompt-publish-dialog.component.html',
})
export class PromptPublishDialogComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IPromptPublishDialogData = inject(DynamicDialogConfig).data;

    readonly version = this._data.version;
    readonly publishedVersion = this._data.publishedVersion;
    readonly noteMaxLength = PROMPT_NOTE_MAX_LENGTH;
    readonly note = new FormControl(this._data.note, { nonNullable: true });

    close() {
        this._ref.close(null);
    }

    publish() {
        this._ref.close(this.note.value ?? '');
    }
}
