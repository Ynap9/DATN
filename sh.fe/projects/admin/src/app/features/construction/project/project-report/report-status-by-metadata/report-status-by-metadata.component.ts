import { Component, computed, input } from '@angular/core';
import { IProjectAcceptanceSummary, IViewProjectAcceptance } from '@/models/project.model';
import { formatNumber, formatPercent, REPORT_COLORS, toPercent } from '../project-report.constants';

// label: chú thích; text: chữ trong aria-label của thanh
interface ISegmentDef {
    label: string;
    text: string;
    color: string;
    count: (row: IProjectAcceptanceSummary) => number;
}

// Phần còn lại của thanh (nền) là Chưa kiểm
const NOT_CHECKED_LEGEND = { label: 'Chưa kiểm', text: 'chưa kiểm', color: REPORT_COLORS.NOT_CHECKED };

const QA_SEGMENTS: ISegmentDef[] = [
    { label: 'QA đạt', text: 'QA đạt', color: REPORT_COLORS.QA_PASSED, count: (row) => row.totalQaPassed },
    { label: 'Chưa QA', text: 'chưa QA', color: REPORT_COLORS.QA_PENDING, count: (row) => row.totalQaNotReviewed },
    { label: 'Trả lại', text: 'trả lại', color: REPORT_COLORS.RETURNED, count: (row) => row.totalReturned },
];

// Dự án không QA: chỉ Đã kiểm / Chưa kiểm (Chưa kiểm gồm cả bị trả)
const CHECK_SEGMENTS: ISegmentDef[] = [{ label: 'Đã kiểm', text: 'đã kiểm', color: REPORT_COLORS.CHECKED, count: (row) => row.totalChecked }];

interface IStatusRow {
    key: string;
    label: string;
    isTotal: boolean;
    summary: string;
    percent: string;
    ariaLabel: string;
    segments: { color: string; width: number }[];
}

@Component({
    selector: 'app-report-status-by-metadata',
    templateUrl: './report-status-by-metadata.component.html',
    styleUrl: './report-status-by-metadata.component.scss',
})
export class ReportStatusByMetadataComponent {
    acceptance = input.required<IViewProjectAcceptance>();
    isQaEnabled = input.required<boolean>();

    readonly notCheckedColor = NOT_CHECKED_LEGEND.color;

    readonly legend = computed(() => [...this.segmentDefs(), NOT_CHECKED_LEGEND]);

    // Mỗi metadata (kể cả đã gỡ) một dòng, cuối là dòng Toàn dự án
    readonly rows = computed<IStatusRow[]>(() => {
        const acceptance = this.acceptance();
        return [
            ...acceptance.documentTypes.map((row) =>
                this.buildRow(row, `${row.documentTypeId}`, (row.documentTypeName ?? '—') + (row.isRemoved ? ' (đã gỡ)' : ''), false),
            ),
            this.buildRow(acceptance.total, 'total', 'Toàn dự án', true),
        ];
    });

    private segmentDefs() {
        return this.isQaEnabled() ? QA_SEGMENTS : CHECK_SEGMENTS;
    }

    private buildRow(row: IProjectAcceptanceSummary, key: string, label: string, isTotal: boolean): IStatusRow {
        const defs = this.segmentDefs();
        const counts = defs.map((def) => def.count(row));
        const notChecked = row.totalSubmissions - counts.reduce((sum, count) => sum + count, 0);
        const parts = [...defs.map((def, index) => `${formatNumber(counts[index])} ${def.text}`), `${formatNumber(notChecked)} ${NOT_CHECKED_LEGEND.text}`];
        const percent = this.isQaEnabled() ? row.qaPassedPercent : toPercent(row.totalChecked, row.totalSubmissions);
        return {
            key,
            label,
            isTotal,
            summary: `${formatNumber(row.totalSubmissions)} văn bản`,
            percent: `${formatPercent(percent)} ${this.isQaEnabled() ? 'đạt' : 'đã kiểm'}`,
            ariaLabel: `${label}: ${parts.join(', ')}`,
            segments: defs
                .map((def, index) => ({ color: def.color, width: toPercent(counts[index], row.totalSubmissions) }))
                .filter((segment) => segment.width > 0),
        };
    }
}
