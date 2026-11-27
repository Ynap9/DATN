import { Component, DestroyRef, inject, input } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { catchError, forkJoin, map, merge, Observable, of, Subject } from 'rxjs';
import { IFindStats, IProgressUserStats, IQaEntryUserStats } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { IBaseResponsePagingData, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { bindReportBlock, createReportBlock, IReportBlock, TOP_ENTRY_USERS } from '../project-report.constants';
import {
    DATA_PRODUCTIVITY_COLUMNS,
    DATA_QUALITY_PRODUCTIVITY_COLUMNS,
    DataProductivityRow,
    IProductivityQuality,
    ProductivityRow,
    QA_PRODUCTIVITY_COLUMNS,
    QaProductivityRow,
    toDataRows,
    toQaRows,
} from './productivity-columns';

// Một lần lấy đủ người để ghép chất lượng theo userId
const QUALITY_PAGE_SIZE = 200;
const DIALOG_PAGE_SIZES = [10, 25, 50];

const PRODUCTIVITY_TAB = {
    DATA: 'data',
    QA: 'qa',
} as const;

type ProductivityTab = (typeof PRODUCTIVITY_TAB)[keyof typeof PRODUCTIVITY_TAB];

const SUBTITLE_SUFFIX = 'Admin, Leader kiểm hộ tính cho người đang giữ văn bản';
const SUBTITLE_WITH_QA = `Theo lượt kiểm / chấm trong khoảng · ${SUBTITLE_SUFFIX}`;
const SUBTITLE_WITHOUT_QA = `Theo lượt kiểm trong khoảng · ${SUBTITLE_SUFFIX}`;

// Kết quả một lần tải: năng suất + chất lượng theo userId (null = không tải / API chất lượng lỗi)
interface IProductivity {
    stats: IProgressUserStats;
    quality: Map<string, IProductivityQuality> | null;
}

// Nằm trong @defer của trang: tự tải khi cuộn tới; bảng hiện 5 dòng đầu của tab đang chọn, "Xem tất cả" mở dialog phân trang ở FE
@Component({
    selector: 'app-report-productivity',
    imports: [ButtonModule, DialogModule, SkeletonModule, DataTable],
    templateUrl: './report-productivity.component.html',
    styleUrl: './report-productivity.component.scss',
})
export class ReportProductivityComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _retry$ = new Subject<IFindStats>();

    filter = input.required<IFindStats>();
    isQaEnabled = input.required<boolean>();
    canViewQaStats = input.required<boolean>();

    readonly tabs = PRODUCTIVITY_TAB;
    readonly dialogPageSizes = DIALOG_PAGE_SIZES;
    readonly block: IReportBlock<IProductivity> = createReportBlock();
    activeTab: ProductivityTab = PRODUCTIVITY_TAB.DATA;
    dataRows: DataProductivityRow[] = [];
    qaRows: QaProductivityRow[] = [];
    topRows: ProductivityRow[] = [];
    dialogRows: ProductivityRow[] = [];
    dialogVisible = false;
    dialogPageNumber = this.START_PAGE_NUMBER;
    dialogPageSize = DIALOG_PAGE_SIZES[0];

    // Tab QA chỉ có khi BE báo dự án có QA
    get hasQaTab() {
        return this.block.data?.stats.isQaEnabled ?? false;
    }

    get activeRows(): ProductivityRow[] {
        return this.activeTab === PRODUCTIVITY_TAB.QA ? this.qaRows : this.dataRows;
    }

    get columns() {
        if (this.activeTab === PRODUCTIVITY_TAB.QA) return QA_PRODUCTIVITY_COLUMNS;
        return this.isQaEnabled() && this.canViewQaStats() ? DATA_QUALITY_PRODUCTIVITY_COLUMNS : DATA_PRODUCTIVITY_COLUMNS;
    }

    get subtitle() {
        return this.isQaEnabled() ? SUBTITLE_WITH_QA : SUBTITLE_WITHOUT_QA;
    }

    get dialogTitle() {
        return `Năng suất · ${this.activeTab === PRODUCTIVITY_TAB.QA ? 'QA' : 'Nhập liệu'}`;
    }

    // Dòng chưa có số: cả dòng chữ màu phụ (style ở scss)
    readonly rowClass = (row: ProductivityRow) => (row.isZero ? 'productivity-row--zero' : '');

    constructor() {
        super();
        bindReportBlock(
            this.block,
            merge(toObservable(this.filter), this._retry$),
            (filter: IFindStats) => this.loadProductivity(filter),
            (res) => this.isResponseSucceed(res),
            this._destroyRef,
            (productivity) => this.showProductivity(productivity),
        );
    }

    retry() {
        this._retry$.next(this.filter());
    }

    selectTab(tab: ProductivityTab) {
        this.activeTab = tab;
        this.topRows = this.activeRows.slice(0, TOP_ENTRY_USERS);
    }

    openAll() {
        this.dialogVisible = true;
        this.dialogPageNumber = this.START_PAGE_NUMBER;
        this.dialogPageSize = DIALOG_PAGE_SIZES[0];
        this.updateDialogRows();
    }

    onDialogPageChange(event: PaginatorState) {
        this.dialogPageNumber = (event.page ?? 0) + 1;
        this.dialogPageSize = event.rows ?? this.dialogPageSize;
        this.updateDialogRows();
    }

    // Năng suất và chất lượng gọi song song; API chất lượng lỗi thì chỉ mất 2 cột (không toast thêm), bảng vẫn hiện
    private loadProductivity(filter: IFindStats): Observable<IBaseResponseWithData<IProductivity>> {
        const quality$ =
            this.isQaEnabled() && this.canViewQaStats()
                ? this._submissionService
                      .qaStatsEntryUsers({ ...filter, pageNumber: this.START_PAGE_NUMBER, pageSize: QUALITY_PAGE_SIZE, orderBy: 'review' }, true)
                      .pipe(catchError(() => of(null)))
                : of(null);
        return forkJoin([this._submissionService.findProgressUserStats(filter), quality$]).pipe(
            map(([progress, quality]) => ({
                ...progress,
                data: { stats: progress.data, quality: quality?.status === 1 ? this.toQualityMap(quality.data) : null },
            })),
        );
    }

    private toQualityMap(page: IBaseResponsePagingData<IQaEntryUserStats>) {
        return new Map(
            page.items.filter((item) => item.userId).map((item) => [item.userId!, { firstPassRate: item.firstPassRate, fieldAccuracyRate: item.fieldAccuracyRate }]),
        );
    }

    private showProductivity(productivity: IProductivity) {
        this.dataRows = toDataRows(productivity.stats.dataUsers, productivity.quality);
        this.qaRows = toQaRows(productivity.stats.qaUsers);
        this.selectTab(productivity.stats.isQaEnabled ? this.activeTab : PRODUCTIVITY_TAB.DATA);
    }

    private updateDialogRows() {
        const start = (this.dialogPageNumber - 1) * this.dialogPageSize;
        this.dialogRows = this.activeRows.slice(start, start + this.dialogPageSize);
    }
}
