import { Component, computed, input } from '@angular/core';
import { IProjectAcceptanceSummary } from '@/models/project.model';
import { IProgressDaily, IQaStatsSummary } from '@/models/submission.model';
import { formatNumber, formatPercent, REPORT_COLORS, toPercent } from '../project-report.constants';

const RECENT_DAYS = 7;

// Màu chữ phụ theo token --app-<tone>
type KpiTone = 'blue' | 'green' | 'amber';

// Một thẻ số; note = null thì ẩn dòng phụ (chưa có số hoặc thiếu quyền), percent = null thì không có thanh
interface IKpiCard {
    label: string;
    value: string;
    extra?: string;
    extraTone?: KpiTone;
    note: string | null;
    percent: number | null;
    color?: string;
}

@Component({
    selector: 'app-report-kpi',
    templateUrl: './report-kpi.component.html',
    styleUrl: './report-kpi.component.scss',
})
export class ReportKpiComponent {
    // Dòng nghiệm thu của metadata đang chọn; total = dòng toàn dự án
    summary = input.required<IProjectAcceptanceSummary>();
    total = input.required<IProjectAcceptanceSummary>();
    isQaEnabled = input.required<boolean>();
    // null: chưa tải hoặc không có quyền xem
    days = input<IProgressDaily[] | null>(null);
    qaSummary = input<IQaStatsSummary | null>(null);

    // Dự án không QA ẩn thẻ QA đạt và mọi chữ "QA"
    readonly cards = computed<IKpiCard[]>(() => {
        const summary = this.summary();
        const isQaEnabled = this.isQaEnabled();
        const days = this.days();
        const qaSummary = this.qaSummary();
        const checkedPercent = toPercent(summary.totalChecked, summary.totalSubmissions);
        const recentChecked = days?.slice(-RECENT_DAYS).reduce((sum, day) => sum + day.checkedCount, 0);
        const cards: IKpiCard[] = [
            {
                label: 'Tổng văn bản',
                value: formatNumber(summary.totalSubmissions),
                note: `Toàn dự án ${formatNumber(this.total().totalSubmissions)} văn bản`,
                percent: null,
            },
            {
                label: 'Đã kiểm',
                value: formatNumber(summary.totalChecked),
                extra: formatPercent(checkedPercent),
                extraTone: 'blue',
                note: recentChecked === undefined ? null : `+${formatNumber(recentChecked)} trong ${RECENT_DAYS} ngày gần nhất`,
                percent: checkedPercent,
                color: REPORT_COLORS.CHECKED,
            },
        ];
        if (isQaEnabled) {
            cards.push({
                label: 'QA đạt',
                value: formatNumber(summary.totalQaPassed),
                extra: formatPercent(summary.qaPassedPercent),
                extraTone: 'green',
                note: qaSummary ? `Đạt ngay lần đầu ${formatPercent(qaSummary.firstPassRate)}` : null,
                percent: summary.qaPassedPercent,
                color: REPORT_COLORS.QA_PASSED,
            });
        }
        cards.push({
            label: 'Cần xử lý',
            value: formatNumber(summary.totalReturned),
            extra: 'bị trả lại',
            extraTone: 'amber',
            note: isQaEnabled ? `${formatNumber(summary.totalQaNotReviewed)} văn bản đang chờ QA chấm` : null,
            percent: toPercent(summary.totalReturned, summary.totalSubmissions),
            color: REPORT_COLORS.RETURNED,
        });
        return cards;
    });
}
