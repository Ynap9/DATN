import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShCalendarComponent, ShSelectComponent } from '@sh/ui';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap } from 'rxjs';
import { IDeadlineProjectFilter, IProjectDeadlineNotification } from '@/models/project-deadline.model';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import {
    DEADLINE_PAGE_SIZE,
    DEADLINE_RESULT_FILTER,
    DEADLINE_RESULT_FILTERS,
    DEADLINE_SEARCH_DEBOUNCE_MS,
    DEADLINE_TYPE_ALL,
    DEADLINE_TYPE_FILTERS,
    DeadlineResultFilter,
    getDaysLeftBadge,
    getDeadlineTypeBadge,
    toIsSuccess,
} from '@/shared/constants/project-deadline.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn } from '@/shared/models/data-table.models';
import { DeadlineMailProjectCellComponent } from '../cells/deadline-mail-project-cell.component';
import { DeadlineMailResultCellComponent } from '../cells/deadline-mail-result-cell.component';
import { DEADLINE_QUERY, readDeadlineQuery, toSuccessParam, updateDeadlineQuery } from '../deadline-query';

const NOWRAP_STYLE = 'white-space: nowrap';
const DATE_TIME_FORMAT = 'dd/MM/yyyy HH:mm';

// Tab "Lịch sử mail": mail báo hạn hệ thống đã gửi, mới nhất trước (Task 73.7)
@Component({
    selector: 'app-deadline-mail-tab',
    imports: [SharedImports, DataTable, ShSelectComponent, ShCalendarComponent, IconFieldModule, InputIconModule],
    templateUrl: './deadline-mail-tab.component.html',
})
export class DeadlineMailTabComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _load$ = new Subject<void>();

    readonly typeOptions = DEADLINE_TYPE_FILTERS;
    readonly resultOptions = DEADLINE_RESULT_FILTERS;
    readonly keyword = new FormControl('', { nonNullable: true });
    readonly columns = this.buildColumns();

    // Bộ lọc nằm trên URL: đọc một lần khi dựng tab (kể cả dự án từ "Xem mail"), đổi thì ghi lại (F5 giữ)
    project: IDeadlineProjectFilter | null = null;
    type = DEADLINE_TYPE_ALL;
    result: DeadlineResultFilter = DEADLINE_RESULT_FILTER.ALL;
    fromDate: string | null = null;
    toDate: string | null = null;
    rows: IProjectDeadlineNotification[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = DEADLINE_PAGE_SIZE;
    override totalRecords = 0;

    get searchKeyword() {
        return this.keyword.value.trim();
    }

    // BE trả 400 khi từ ngày sau đến ngày nên không gọi
    get isDateRangeInvalid() {
        return !!this.fromDate && !!this.toDate && this.fromDate > this.toDate;
    }

    override ngOnInit(): void {
        const query = readDeadlineQuery(this._activatedRoute.snapshot.queryParamMap);
        this.project = query.project;
        this.type = query.type;
        this.result = query.result;
        this.fromDate = query.from;
        this.toDate = query.to;
        this.keyword.setValue(query.keyword, { emitEvent: false });
        this._load$
            .pipe(
                switchMap(() => {
                    this.loading = true;
                    return this._projectService
                        .findDeadlineNotificationPaging({
                            pageNumber: this.pageNumber,
                            pageSize: this.pageSize,
                            keyword: this.searchKeyword,
                            projectId: this.project?.projectId,
                            notificationType: this.type || undefined,
                            isSuccess: toIsSuccess(this.result),
                            fromDate: this.fromDate ?? undefined,
                            toDate: this.toDate ?? undefined,
                        })
                        .pipe(catchError(() => of(null)));
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                this.loading = false;
                if (!res || !this.isResponseSucceed(res)) return;
                this.totalRecords = res.data.totalItems;
                this.rows = res.data.items;
            });
        this.keyword.valueChanges
            .pipe(
                map((keyword) => keyword.trim()),
                distinctUntilChanged(),
                debounceTime(DEADLINE_SEARCH_DEBOUNCE_MS),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((keyword) => {
                updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.KEYWORD]: keyword });
                this.applyFilter();
            });
        this.load();
    }

    load() {
        this._load$.next();
    }

    // sh-select / sh-calendar phát lại giá trị lúc khởi tạo nên chỉ lọc lại khi giá trị đổi thật
    onTypeChange(type: number) {
        if (type === this.type) return;
        this.type = type;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.TYPE]: type || null });
        this.applyFilter();
    }

    onResultChange(result: DeadlineResultFilter) {
        if (result === this.result) return;
        this.result = result;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.SUCCESS]: toSuccessParam(result) });
        this.applyFilter();
    }

    onFromDateChange(fromDate: string | null) {
        if ((fromDate || null) === this.fromDate) return;
        this.fromDate = fromDate || null;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.FROM]: this.fromDate });
        this.applyFilter();
    }

    onToDateChange(toDate: string | null) {
        if ((toDate || null) === this.toDate) return;
        this.toDate = toDate || null;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.TO]: this.toDate });
        this.applyFilter();
    }

    clearProject() {
        this.project = null;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.PROJECT]: null, [DEADLINE_QUERY.PROJECT_NAME]: null });
        this.applyFilter();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    private applyFilter() {
        this.pageNumber = this.START_PAGE_NUMBER;
        if (!this.isDateRangeInvalid) {
            this.load();
        }
    }

    private buildColumns(): IColumn<IProjectDeadlineNotification>[] {
        return [
            { header: 'Thời gian gửi', field: 'createdDate', cellViewType: CellViewTypes.DATE, dateFormat: DATE_TIME_FORMAT, cellStyle: NOWRAP_STYLE },
            {
                header: 'Dự án',
                field: 'projectName',
                cellViewType: CellViewTypes.CUSTOM_COMP,
                customComponent: DeadlineMailProjectCellComponent,
                headerContainerStyle: 'min-width: 240px',
            },
            { header: 'Loại', key: 'notificationType', cellViewType: CellViewTypes.BADGE, badge: (row) => getDeadlineTypeBadge(row.notificationType) },
            { header: 'Ngày kết thúc lúc gửi', field: 'endedAt', cellViewType: CellViewTypes.DATE, cellStyle: NOWRAP_STYLE },
            { header: 'Còn lại lúc gửi', key: 'daysLeft', cellViewType: CellViewTypes.BADGE, badge: (row) => getDaysLeftBadge(row.daysLeft) },
            { header: 'Người nhận', field: 'toAddress', headerContainerStyle: 'min-width: 200px' },
            { header: 'Kết quả', key: 'isSuccess', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: DeadlineMailResultCellComponent },
        ];
    }
}
