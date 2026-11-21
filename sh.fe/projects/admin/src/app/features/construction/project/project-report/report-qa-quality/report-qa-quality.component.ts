import { Component, computed, ElementRef, inject, input, output } from '@angular/core';
import { ChartData, ChartOptions } from 'chart.js';
import { ButtonModule } from 'primeng/button';
import { ChartModule } from 'primeng/chart';
import { SkeletonModule } from 'primeng/skeleton';
import { IQaStatsSummary } from '@/models/submission.model';
import { formatNumber, formatPercent, readCssVar, REPORT_COLORS } from '../project-report.constants';

const MAX_PERCENT = 100;

@Component({
    selector: 'app-report-qa-quality',
    imports: [ChartModule, ButtonModule, SkeletonModule],
    templateUrl: './report-qa-quality.component.html',
    styleUrl: './report-qa-quality.component.scss',
})
export class ReportQaQualityComponent {
    private readonly _element = inject(ElementRef<HTMLElement>).nativeElement;

    summary = input<IQaStatsSummary | null>(null);
    documentTypeName = input.required<string>();
    rangeLabel = input.required<string>();
    loading = input(false);
    error = input(false);
    retry = output<void>();

    readonly formatNumber = formatNumber;
    readonly formatPercent = formatPercent;

    readonly subtitle = computed(() => {
        const parts = [this.documentTypeName(), this.rangeLabel()];
        const summary = this.summary();
        if (summary) parts.push(`${formatNumber(summary.reviewCount)} lượt chấm`);
        return parts.filter(Boolean).join(' · ');
    });

    // Donut = tỷ lệ đạt ngay lần đầu, phần còn lại là nền
    readonly chartData = computed<ChartData<'doughnut'>>(() => {
        const rate = this.summary()?.firstPassRate ?? 0;
        return {
            datasets: [{ data: [rate, MAX_PERCENT - rate], backgroundColor: [REPORT_COLORS.QA_PASSED, readCssVar(this._element, '--app-bg')], borderWidth: 0 }],
        };
    });

    readonly chartOptions: ChartOptions<'doughnut'> = {
        cutout: '78%',
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
    };
}
