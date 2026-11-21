import { formatDate } from '@angular/common';
import { Component, computed, ElementRef, inject, input, LOCALE_ID, output } from '@angular/core';
import { ChartData, ChartOptions } from 'chart.js';
import { ChartModule } from 'primeng/chart';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { IProgressDailyStats } from '@/models/submission.model';
import { formatNumber, readCssVar, REPORT_COLORS } from '../project-report.constants';

const MAX_X_TICKS = 6;
// Nền mờ dưới đường Đã kiểm (hex alpha ~8%); đường QA không tô
const CHECKED_FILL_ALPHA = '14';
const WEEKDAY_FORMAT = new Intl.DateTimeFormat('vi-VN', { weekday: 'long' });

@Component({
    selector: 'app-report-daily-chart',
    imports: [ChartModule, ButtonModule, SkeletonModule],
    templateUrl: './report-daily-chart.component.html',
    styleUrl: './report-daily-chart.component.scss',
})
export class ReportDailyChartComponent {
    private readonly _locale = inject(LOCALE_ID);
    private readonly _element = inject(ElementRef<HTMLElement>).nativeElement;

    stats = input<IProgressDailyStats | null>(null);
    isQaEnabled = input.required<boolean>();
    loading = input(false);
    error = input(false);
    retry = output<void>();

    readonly formatNumber = formatNumber;
    readonly colors = REPORT_COLORS;

    readonly isEmpty = computed(() => !!this.stats()?.days.every((day) => !day.checkedCount && !day.qaPassedCount));

    readonly chartData = computed<ChartData<'line'>>(() => {
        const days = this.stats()?.days ?? [];
        return {
            labels: days.map((day) => formatDate(day.date, 'dd/MM', this._locale)),
            datasets: [
                {
                    label: 'Đã kiểm',
                    data: days.map((day) => day.checkedCount),
                    borderColor: REPORT_COLORS.CHECKED,
                    backgroundColor: REPORT_COLORS.CHECKED + CHECKED_FILL_ALPHA,
                    fill: true,
                },
                ...(this.isQaEnabled()
                    ? [{ label: 'QA đạt', data: days.map((day) => day.qaPassedCount), borderColor: REPORT_COLORS.QA_PASSED, fill: false }]
                    : []),
            ],
        };
    });

    // Tooltip theo ngày (mode index), trục Y từ 0 số nguyên, trục X tối đa ~6 nhãn; chú thích vẽ ở đầu khối
    readonly chartOptions = computed<ChartOptions<'line'>>(() => {
        const days = this.stats()?.days ?? [];
        const muted = readCssVar(this._element, '--app-muted');
        const line = readCssVar(this._element, '--app-line');
        return {
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            elements: { line: { borderWidth: 2, cubicInterpolationMode: 'monotone' }, point: { radius: 0, hoverRadius: 4 } },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: (items) => this.formatDayTitle(days[items[0].dataIndex].date),
                        label: (item) => `${item.dataset.label}: ${formatNumber(item.parsed.y ?? 0)}`,
                    },
                },
            },
            scales: {
                x: { grid: { display: false }, ticks: { color: muted, maxTicksLimit: MAX_X_TICKS, maxRotation: 0 } },
                y: { beginAtZero: true, grid: { color: line }, border: { display: false }, ticks: { color: muted, precision: 0 } },
            },
        };
    });

    // "Thứ Sáu, 13/11"
    private formatDayTitle(date: string) {
        const weekday = WEEKDAY_FORMAT.format(new Date(date));
        return `${weekday.charAt(0).toUpperCase()}${weekday.slice(1)}, ${formatDate(date, 'dd/MM', this._locale)}`;
    }
}
