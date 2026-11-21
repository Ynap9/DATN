import { Component, DestroyRef, inject, input } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { map, merge, Subject } from 'rxjs';
import { IFindPagingQaEntryUserStats, IFindStats, IQaEntryUserStats } from '@/models/submission.model';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { IBaseResponsePagingData } from '@/shared/models/request-paging.base.models';
import { bindReportBlock, createReportBlock, IReportBlock, TOP_ENTRY_USERS } from '../project-report.constants';
import { ENTRY_USER_COLUMNS, EntryUserRow, toEntryUserRows } from './entry-user-columns';

// Sắp theo số văn bản được chấm
const ORDER_BY_REVIEW = 'review' as const;
const DIALOG_PAGE_SIZES = [10, 25, 50];

type EntryUserPage = IBaseResponsePagingData<IQaEntryUserStats>;

// Nằm trong @defer của trang: tự tải top 5 khi cuộn tới; "Xem tất cả" mở dialog phân trang qua cùng API
@Component({
    selector: 'app-report-entry-users',
    imports: [ButtonModule, DialogModule, SkeletonModule, DataTable],
    templateUrl: './report-entry-users.component.html',
    styleUrl: './report-entry-users.component.scss',
})
export class ReportEntryUsersComponent extends BaseComponent {
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _retry$ = new Subject<IFindStats>();
    private readonly _allQuery$ = new Subject<IFindPagingQaEntryUserStats>();

    filter = input.required<IFindStats>();

    readonly columns = ENTRY_USER_COLUMNS;
    readonly dialogPageSizes = DIALOG_PAGE_SIZES;
    readonly top: IReportBlock<EntryUserPage> = createReportBlock();
    readonly all: IReportBlock<EntryUserPage> = createReportBlock();
    topRows: EntryUserRow[] = [];
    allRows: EntryUserRow[] = [];
    dialogVisible = false;
    allPageNumber = this.START_PAGE_NUMBER;
    allPageSize = DIALOG_PAGE_SIZES[0];

    get totalItems() {
        return this.top.data?.totalItems ?? 0;
    }

    get canViewAll() {
        return this.totalItems > TOP_ENTRY_USERS;
    }

    constructor() {
        super();
        bindReportBlock(
            this.top,
            merge(toObservable(this.filter), this._retry$).pipe(
                map((query) => ({ ...query, pageNumber: this.START_PAGE_NUMBER, pageSize: TOP_ENTRY_USERS, orderBy: ORDER_BY_REVIEW })),
            ),
            (query: IFindPagingQaEntryUserStats) => this._submissionService.qaStatsEntryUsers(query),
            (res) => this.isResponseSucceed(res),
            this._destroyRef,
            (page) => (this.topRows = toEntryUserRows(page.items)),
        );
        bindReportBlock(
            this.all,
            this._allQuery$,
            (query) => this._submissionService.qaStatsEntryUsers(query),
            (res) => this.isResponseSucceed(res),
            this._destroyRef,
            (page) => (this.allRows = toEntryUserRows(page.items)),
        );
    }

    retry() {
        this._retry$.next(this.filter());
    }

    openAll() {
        this.dialogVisible = true;
        this.allPageNumber = this.START_PAGE_NUMBER;
        this.allPageSize = DIALOG_PAGE_SIZES[0];
        this.loadAll();
    }

    onAllPageChange(event: PaginatorState) {
        this.allPageNumber = (event.page ?? 0) + 1;
        this.allPageSize = event.rows ?? this.allPageSize;
        this.loadAll();
    }

    private loadAll() {
        this._allQuery$.next({ ...this.filter(), pageNumber: this.allPageNumber, pageSize: this.allPageSize, orderBy: ORDER_BY_REVIEW });
    }
}
