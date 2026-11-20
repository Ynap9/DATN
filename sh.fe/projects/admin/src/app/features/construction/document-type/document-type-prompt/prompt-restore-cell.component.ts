import { Component, inject, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { IPromptTemplateVersion } from '@/models/prompt-template.model';
import { TBL_CUSTOM_COMP_EMIT } from '@/shared/components/data-table/data-table.component';
import { PROMPT_TEMPLATE_STATUS } from '@/shared/constants/prompt-template.constants';

export const PROMPT_RESTORE_ACTION = 'restore';

// Câu lỗi 910 của BE, hiện sẵn khi đang có nháp để khỏi bấm rồi mới biết
const DRAFT_EXISTS_MESSAGE = 'Đang có bản nháp, hãy bỏ nháp hoặc xuất bản trước';

export type PromptVersionRow = IPromptTemplateVersion & { hasDraft: boolean };

// Ô "Khôi phục thành nháp" của bảng Lịch sử: ẩn với bản nháp, khoá kèm tooltip khi đang có nháp (TblAction không có trạng thái khoá)
@Component({
    selector: 'app-prompt-restore-cell',
    imports: [ButtonModule, TooltipModule],
    template: `
        @if (row().status !== draftStatus) {
            <span [pTooltip]="row().hasDraft ? draftExistsMessage : ''" tooltipPosition="left">
                <p-button label="Khôi phục thành nháp" icon="pi pi-replay" severity="secondary" [text]="true" size="small" styleClass="whitespace-nowrap" [disabled]="row().hasDraft"
                    (onClick)="restore()" />
            </span>
        }
    `,
})
export class PromptRestoreCellComponent {
    private readonly _tableEmit = inject(TBL_CUSTOM_COMP_EMIT);

    row = input.required<PromptVersionRow>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();

    readonly draftStatus = PROMPT_TEMPLATE_STATUS.DRAFT;
    readonly draftExistsMessage = DRAFT_EXISTS_MESSAGE;

    restore() {
        this._tableEmit.emit({ type: PROMPT_RESTORE_ACTION, row: this.row() });
    }
}
