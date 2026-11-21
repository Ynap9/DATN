import { Component, computed, input } from '@angular/core';
import { IQaEntryUserStats } from '@/models/submission.model';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn } from '@/shared/models/data-table.models';
import { formatPercent, REPORT_COLORS } from '../project-report.constants';

// Dòng bảng Năng suất: thêm tên hiển thị và % đạt lần đầu đã định dạng (thẻ và dialog dùng chung)
export type EntryUserRow = IQaEntryUserStats & { displayName: string; firstPassText: string };

export function toEntryUserRows(items: IQaEntryUserStats[]): EntryUserRow[] {
    return items.map((item) => ({
        ...item,
        displayName: item.fullName || item.userName || '—',
        firstPassText: formatPercent(item.firstPassRate),
    }));
}

// Chữ cái đầu của từ đầu và từ cuối: "Nguyễn Thị Lan" → "NL"
function getInitials(name: string) {
    const words = name.trim().split(/\s+/).filter(Boolean);
    if (!words.length) return '';
    return (words[0][0] + (words.length > 1 ? words[words.length - 1][0] : '')).toUpperCase();
}

@Component({
    selector: 'app-entry-user-name-cell',
    template: `
        <span class="flex items-center gap-2.5">
            <span class="inline-flex size-7 flex-none items-center justify-center rounded-full bg-[var(--app-bg)] text-[11px] font-semibold text-muted-color"
                aria-hidden="true">{{ initials() }}</span>
            <span class="whitespace-nowrap">{{ row().displayName }}</span>
        </span>
    `,
})
export class EntryUserNameCellComponent {
    row = input.required<EntryUserRow>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();

    initials = computed(() => getInitials(this.row().displayName));
}

@Component({
    selector: 'app-entry-user-accuracy-cell',
    template: `
        <span class="flex items-center gap-2">
            <span class="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--app-bg)]">
                <span class="block h-full" [style.width.%]="row().fieldAccuracyRate" [style.background]="color"></span>
            </span>
            <span class="w-12 text-right font-medium tabular-nums">{{ text() }}</span>
        </span>
    `,
})
export class EntryUserAccuracyCellComponent {
    row = input.required<EntryUserRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly color = REPORT_COLORS.QA_PASSED;
    text = computed(() => formatPercent(this.row().fieldAccuracyRate));
}

const RIGHT_STYLE = 'text-align: right';

export const ENTRY_USER_COLUMNS: IColumn<EntryUserRow>[] = [
    { header: 'Người nhập', key: 'user', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: EntryUserNameCellComponent },
    {
        header: 'Văn bản',
        field: 'reviewedDocumentCount',
        cellViewType: CellViewTypes.NUMBER,
        headerContainerStyle: `width: 90px; ${RIGHT_STYLE}`,
        cellStyle: RIGHT_STYLE,
    },
    { header: 'Đạt lần đầu', field: 'firstPassText', headerContainerStyle: `width: 110px; ${RIGHT_STYLE}`, cellStyle: RIGHT_STYLE },
    {
        header: 'Độ chính xác ô',
        key: 'accuracy',
        cellViewType: CellViewTypes.CUSTOM_COMP,
        customComponent: EntryUserAccuracyCellComponent,
        headerContainerStyle: 'width: 170px',
    },
];
