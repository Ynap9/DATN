import { Component, DestroyRef, inject, input } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { merge, Subject } from 'rxjs';
import { IFindStats, IQaFieldStats } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { bindReportBlock, createReportBlock, formatNumber, IReportBlock, REPORT_COLORS, TOP_WRONG_FIELDS, toPercent } from '../project-report.constants';

// Nằm trong @defer của trang: tự tải khi cuộn tới, tải lại mỗi khi trang đổi lọc / Làm mới (filter là object mới)
@Component({
    selector: 'app-report-field-errors',
    imports: [ButtonModule, SkeletonModule],
    templateUrl: './report-field-errors.component.html',
    styleUrl: './report-field-errors.component.scss',
})
export class ReportFieldErrorsComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _retry$ = new Subject<IFindStats>();

    filter = input.required<IFindStats>();

    readonly block: IReportBlock<IQaFieldStats[]> = createReportBlock();
    readonly barColor = REPORT_COLORS.RETURNED;
    readonly formatNumber = formatNumber;
    rows: (IQaFieldStats & { width: number })[] = [];

    constructor() {
        super();
        bindReportBlock(
            this.block,
            merge(toObservable(this.filter), this._retry$),
            (query) => this._submissionService.qaStatsFields(query),
            (res) => this.isResponseSucceed(res),
            this._destroyRef,
            (data) => (this.rows = this.buildRows(data)),
        );
    }

    retry() {
        this._retry$.next(this.filter());
    }

    // Chỉ trường bị đánh sai, nhiều nhất trước; thanh so với trường sai nhiều nhất
    private buildRows(data: IQaFieldStats[]) {
        const fields = data
            .filter((field) => field.wrongCount > 0)
            .sort((a, b) => b.wrongCount - a.wrongCount)
            .slice(0, TOP_WRONG_FIELDS);
        const max = fields[0]?.wrongCount ?? 0;
        return fields.map((field) => ({ ...field, width: toPercent(field.wrongCount, max) }));
    }
}
