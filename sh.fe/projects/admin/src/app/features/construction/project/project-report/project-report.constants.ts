import { formatDate } from '@angular/common';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, Observable, of, switchMap, tap } from 'rxjs';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';

export const REPORT_RANGE = {
    DAYS_7: '7',
    DAYS_30: '30',
    ALL: 'all',
} as const;

export type ReportRange = (typeof REPORT_RANGE)[keyof typeof REPORT_RANGE];

export const REPORT_RANGE_OPTIONS: { value: ReportRange; label: string }[] = [
    { value: REPORT_RANGE.DAYS_7, label: '7 ngày' },
    { value: REPORT_RANGE.DAYS_30, label: '30 ngày' },
    { value: REPORT_RANGE.ALL, label: 'Toàn bộ' },
];

export const DEFAULT_REPORT_RANGE: ReportRange = REPORT_RANGE.DAYS_30;

// Màu series của các khối, khai một chỗ duy nhất
export const REPORT_COLORS = {
    CHECKED: '#2563EB',
    QA_PASSED: '#059669',
    QA_PENDING: '#93C5FD',
    RETURNED: '#F59E0B',
    NOT_CHECKED: '#E2E8F0',
} as const;

// Khớp QA_STATS_MAX_DAYS của BE
export const MAX_RANGE_DAYS = 366;

// Số dòng trên thẻ của khối Năng suất / Trường hay sai
export const TOP_ENTRY_USERS = 5;
export const TOP_WRONG_FIELDS = 6;

const RANGE_DAYS: Record<Exclude<ReportRange, 'all'>, number> = {
    [REPORT_RANGE.DAYS_7]: 7,
    [REPORT_RANGE.DAYS_30]: 30,
};

// Định dạng ngày gửi BE; chỉ gồm số nên không phụ thuộc locale
const DATE_PARAM_FORMAT = 'yyyy-MM-dd';
const DATE_PARAM_LOCALE = 'en-US';

// Trạng thái một khối số liệu: giữ data cũ khi đang tải lại, lỗi chỉ đánh dấu khối đó
export interface IReportBlock<T> {
    data: T | null;
    loading: boolean;
    error: boolean;
}

export function createReportBlock<T>(): IReportBlock<T> {
    return { data: null, loading: false, error: false };
}

// Nối một khối với nguồn lọc: lọc mới huỷ request cũ (switchMap), lỗi HTTP / lỗi BE chỉ đánh dấu khối, luồng không dừng.
// isSucceed là isResponseSucceed của BaseComponent để lỗi theo mã BE vẫn toast như các màn khác
export function bindReportBlock<Q, T>(
    block: IReportBlock<T>,
    query$: Observable<Q>,
    request: (query: Q) => Observable<IBaseResponseWithData<T>>,
    isSucceed: (res: IBaseResponseWithData<T>) => boolean,
    destroyRef: DestroyRef,
    onLoaded?: (data: T) => void,
) {
    query$
        .pipe(
            tap(() => {
                block.loading = true;
                block.error = false;
            }),
            switchMap((query) => request(query).pipe(catchError(() => of(null)))),
            takeUntilDestroyed(destroyRef),
        )
        .subscribe((res) => {
            block.loading = false;
            if (res && isSucceed(res)) {
                block.data = res.data;
                onLoaded?.(res.data);
            } else {
                block.error = true;
            }
        });
}

// Khoảng gửi BE, gồm cả hai đầu. Toàn bộ = từ ngày bắt đầu dự án, tối đa MAX_RANGE_DAYS ngày gần nhất, không sau hôm nay
export function buildRange(range: ReportRange, startedAt: string | null, today: Date) {
    const toDate = addDays(today, 0);
    const earliest = addDays(toDate, -(MAX_RANGE_DAYS - 1));
    let fromDate: Date;
    if (range === REPORT_RANGE.ALL) {
        const started = startedAt ? addDays(new Date(startedAt), 0) : earliest;
        fromDate = new Date(Math.min(Math.max(started.getTime(), earliest.getTime()), toDate.getTime()));
    } else {
        fromDate = addDays(toDate, -(RANGE_DAYS[range] - 1));
    }
    return {
        fromDate: formatDate(fromDate, DATE_PARAM_FORMAT, DATE_PARAM_LOCALE),
        toDate: formatDate(toDate, DATE_PARAM_FORMAT, DATE_PARAM_LOCALE),
    };
}

// "22/10 – 20/11/2026"; khác năm thì ghi năm cả hai đầu
export function formatRangeLabel(fromDate: string, toDate: string, locale: string) {
    const sameYear = new Date(fromDate).getFullYear() === new Date(toDate).getFullYear();
    return `${formatDate(fromDate, sameYear ? 'dd/MM' : 'dd/MM/yyyy', locale)} – ${formatDate(toDate, 'dd/MM/yyyy', locale)}`;
}

const NUMBER_FORMAT = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });
const PERCENT_FORMAT = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 });

// Số kiểu vi-VN: 1.240; 0,13
export function formatNumber(value: number) {
    return NUMBER_FORMAT.format(value);
}

// Phần trăm 0–100 kiểu vi-VN, 1 số lẻ: 70,2%
export function formatPercent(value: number) {
    return `${PERCENT_FORMAT.format(value)}%`;
}

// Tỷ lệ phần trăm part / total; total = 0 thì 0
export function toPercent(part: number, total: number) {
    return total ? (part / total) * 100 : 0;
}

// Đọc token màu --app-* cho chart.js (canvas không dùng được var() của CSS). Token khai báo ở .layout-wrapper,
// không ở :root, nên đọc trên chính phần tử của khối
export function readCssVar(element: Element, name: string) {
    return getComputedStyle(element).getPropertyValue(name).trim();
}

// Bỏ giờ phút, cộng thêm số ngày theo lịch địa phương
function addDays(date: Date, days: number) {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}
