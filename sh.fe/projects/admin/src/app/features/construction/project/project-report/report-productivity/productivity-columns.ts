import { Component, computed, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { IProgressDataUser, IProgressQaUser, IProgressUser } from '@/models/submission.model';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn } from '@/shared/models/data-table.models';
import { formatPercent, REPORT_COLORS } from '../project-report.constants';

const UNASSIGNED_NAME = 'Không gắn người';
const EMPTY_TEXT = '—';
const UNASSIGNED_INITIALS = '?';

const AVERAGE_FORMAT = new Intl.NumberFormat('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

// Chất lượng ghép từ qa-stats/entry-users theo userId (Đạt lần đầu, Độ chính xác ô)
export interface IProductivityQuality {
    firstPassRate: number;
    fieldAccuracyRate: number;
}

// Thêm vào mỗi dòng: tên hiển thị, "TB / ngày" đã định dạng, cờ để tô xám dòng chưa có số
interface IProductivityRowExtra {
    displayName: string;
    isUnassigned: boolean;
    isZero: boolean;
    averageText: string;
}

export type DataProductivityRow = IProgressDataUser & IProductivityRowExtra & { firstPassText: string; fieldAccuracyRate: number | null };
export type QaProductivityRow = IProgressQaUser & IProductivityRowExtra;
export type ProductivityRow = DataProductivityRow | QaProductivityRow;

// TB / ngày = số lượt (văn bản kiểm hoặc lượt chấm) ÷ ngày làm, 1 số lẻ; ngày làm 0 thì "—"
function toRowExtra(user: IProgressUser, count: number, isZero: boolean): IProductivityRowExtra {
    return {
        displayName: user.fullName || user.userName || (user.userId === null ? UNASSIGNED_NAME : EMPTY_TEXT),
        isUnassigned: user.userId === null,
        isZero,
        averageText: user.workingDays ? AVERAGE_FORMAT.format(count / user.workingDays) : EMPTY_TEXT,
    };
}

// quality = null (API chất lượng lỗi) hoặc người không có trong đó → hai cột chất lượng là "—"
export function toDataRows(users: IProgressDataUser[], quality: Map<string, IProductivityQuality> | null): DataProductivityRow[] {
    return users.map((user) => {
        const userQuality = user.userId ? quality?.get(user.userId) : undefined;
        return {
            ...user,
            ...toRowExtra(user, user.documentCount, user.documentCount === 0 && user.fieldCount === 0),
            firstPassText: userQuality ? formatPercent(userQuality.firstPassRate) : EMPTY_TEXT,
            fieldAccuracyRate: userQuality?.fieldAccuracyRate ?? null,
        };
    });
}

export function toQaRows(users: IProgressQaUser[]): QaProductivityRow[] {
    return users.map((user) => ({ ...user, ...toRowExtra(user, user.reviewCount, user.reviewCount === 0) }));
}

// Chữ cái đầu của từ đầu và từ cuối: "Nguyễn Thị Lan" → "NL"
function getInitials(name: string) {
    const words = name.trim().split(/\s+/).filter(Boolean);
    if (!words.length) return '';
    return (words[0][0] + (words.length > 1 ? words[words.length - 1][0] : '')).toUpperCase();
}

@Component({
    selector: 'app-productivity-name-cell',
    imports: [TagModule],
    template: `
        <span class="flex items-center gap-2.5">
            <span class="inline-flex size-7 flex-none items-center justify-center rounded-full bg-[var(--app-bg)] text-[11px] font-semibold text-muted-color"
                aria-hidden="true">{{ initials() }}</span>
            <span class="whitespace-nowrap">{{ row().displayName }}</span>
            @if (row().isRemoved) {
                <p-tag value="Đã gỡ" severity="secondary" [rounded]="true" />
            }
        </span>
    `,
})
export class ProductivityNameCellComponent {
    row = input.required<ProductivityRow>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();

    initials = computed(() => (this.row().isUnassigned ? UNASSIGNED_INITIALS : getInitials(this.row().displayName)));
}

@Component({
    selector: 'app-productivity-accuracy-cell',
    template: `
        @if (row().fieldAccuracyRate !== null) {
            <span class="flex items-center gap-2">
                <span class="h-1.5 flex-1 overflow-hidden rounded-full bg-[var(--app-bg)]">
                    <span class="block h-full" [style.width.%]="row().fieldAccuracyRate" [style.background]="color"></span>
                </span>
                <span class="w-12 text-right font-medium tabular-nums">{{ text() }}</span>
            </span>
        } @else {
            <span class="text-muted-color">{{ emptyText }}</span>
        }
    `,
})
export class ProductivityAccuracyCellComponent {
    row = input.required<DataProductivityRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly color = REPORT_COLORS.QA_PASSED;
    readonly emptyText = EMPTY_TEXT;
    text = computed(() => formatPercent(this.row().fieldAccuracyRate ?? 0));
}

const RIGHT_STYLE = 'text-align: right';

function numberColumn(header: string, field: string, width: number): IColumn {
    return { header, field, cellViewType: CellViewTypes.NUMBER, headerContainerStyle: `width: ${width}px; ${RIGHT_STYLE}`, cellStyle: RIGHT_STYLE };
}

function textColumn(header: string, field: string, width: number): IColumn {
    return { header, field, headerContainerStyle: `width: ${width}px; ${RIGHT_STYLE}`, cellStyle: RIGHT_STYLE };
}

function nameColumn(header: string): IColumn {
    return { header, key: 'user', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: ProductivityNameCellComponent };
}

export const DATA_PRODUCTIVITY_COLUMNS: IColumn[] = [
    nameColumn('Người nhập'),
    numberColumn('Văn bản đã kiểm', 'documentCount', 120),
    numberColumn('Ô có dữ liệu', 'fieldCount', 110),
    numberColumn('Ngày làm', 'workingDays', 90),
    textColumn('TB / ngày', 'averageText', 90),
];

// Chỉ khi dự án có QA và người xem có quyền QaStats
const QUALITY_COLUMNS: IColumn[] = [
    textColumn('Đạt lần đầu', 'firstPassText', 110),
    { header: 'Độ chính xác ô', key: 'accuracy', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: ProductivityAccuracyCellComponent, headerContainerStyle: 'width: 170px' },
];

export const DATA_QUALITY_PRODUCTIVITY_COLUMNS: IColumn[] = [...DATA_PRODUCTIVITY_COLUMNS, ...QUALITY_COLUMNS];

export const QA_PRODUCTIVITY_COLUMNS: IColumn[] = [
    nameColumn('Người chấm'),
    numberColumn('Lượt chấm', 'reviewCount', 100),
    numberColumn('Văn bản', 'reviewDocumentCount', 90),
    numberColumn('Đạt', 'passedCount', 80),
    numberColumn('Không đạt', 'failedCount', 100),
    numberColumn('Ngày làm', 'workingDays', 90),
    textColumn('TB lượt / ngày', 'averageText', 120),
];
