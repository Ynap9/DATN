import { Component, DestroyRef, inject, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { SelectButtonModule } from 'primeng/selectbutton';
import { catchError, debounceTime, distinctUntilChanged, map, of, Subject, switchMap } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IDeadlineProjectFilter, IProjectDeadline } from '@/models/project-deadline.model';
import { IViewProject } from '@/models/project.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import {
    DEADLINE_PAGE_SIZE,
    DEADLINE_SEARCH_DEBOUNCE_MS,
    DEADLINE_TYPE_ALL,
    DEADLINE_TYPE_FILTERS,
    getDaysLeftBadge,
} from '@/shared/constants/project-deadline.constants';
import { getProjectStatusBadge, PROJECT_STATUS, PROJECT_URL } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn, ITableAction, ITableEvent } from '@/shared/models/data-table.models';
import { ProjectFormComponent } from '../../project/project-form/project-form.component';
import { DeadlineLastMailCellComponent } from '../cells/deadline-last-mail-cell.component';
import { DEADLINE_QUERY, readDeadlineQuery, updateDeadlineQuery } from '../deadline-query';

const DEADLINE_ACTIONS = {
    EXTEND: 'extend',
    VIEW_MAILS: 'viewMails',
} as const;

const CENTER_STYLE = 'text-align: center';
const NOWRAP_STYLE = 'white-space: nowrap';

// Tab "Dự án cần chú ý": dự án sắp hoặc đã quá ngày kết thúc, quá hạn lâu nhất lên đầu (Task 73.6)
@Component({
    selector: 'app-deadline-attention-tab',
    imports: [SharedImports, DataTable, SelectButtonModule, IconFieldModule, InputIconModule],
    templateUrl: './deadline-attention-tab.component.html',
})
export class DeadlineAttentionTabComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _load$ = new Subject<void>();

    // Sửa dự án xong (gia hạn): màn cha tải lại thẻ trạng thái job
    readonly extended = output<void>();
    readonly viewMails = output<IDeadlineProjectFilter>();

    readonly typeOptions = DEADLINE_TYPE_FILTERS;
    readonly keyword = new FormControl('', { nonNullable: true });
    readonly columns = this.buildColumns();

    // Bộ lọc nằm trên URL: đọc một lần khi dựng tab, đổi thì ghi lại (F5 giữ)
    type = DEADLINE_TYPE_ALL;
    rows: IProjectDeadline[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = DEADLINE_PAGE_SIZE;
    override totalRecords = 0;
    private _documentTypes: IDocumentTypeDropdown[] = [];

    get searchKeyword() {
        return this.keyword.value.trim();
    }

    override ngOnInit(): void {
        const query = readDeadlineQuery(this._activatedRoute.snapshot.queryParamMap);
        this.type = query.type;
        this.keyword.setValue(query.keyword, { emitEvent: false });
        this._load$
            .pipe(
                switchMap(() => {
                    this.loading = true;
                    return this._projectService
                        .findDeadlinePaging({
                            pageNumber: this.pageNumber,
                            pageSize: this.pageSize,
                            keyword: this.searchKeyword,
                            notificationType: this.type || undefined,
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
        this._documentTypeService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this._documentTypes = res.data;
                }
            });
        this.load();
    }

    load() {
        this._load$.next();
    }

    onTypeChange(type: number) {
        this.type = type;
        updateDeadlineQuery(this.router, this._activatedRoute, { [DEADLINE_QUERY.TYPE]: type || null });
        this.applyFilter();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    onAction(event: ITableEvent<IProjectDeadline>) {
        const project = event.row!;
        if (event.type === TableEventTypes.CELL_CLICK) {
            this.router.navigate([PROJECT_URL, project.projectId]);
        } else if (event.type === DEADLINE_ACTIONS.EXTEND) {
            this.openEditForm(project.projectId);
        } else if (event.type === DEADLINE_ACTIONS.VIEW_MAILS) {
            this.viewMails.emit({ projectId: project.projectId, name: project.name });
        }
    }

    private applyFilter() {
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }

    // Form sửa dự án cần danh sách metadata (tải một lần) và GET {id} để có đủ documentTypeIds
    private openEditForm(projectId: number) {
        this._projectService
            .findById(projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.openForm(res.data);
                }
            });
    }

    private openForm(project: IViewProject) {
        this._dialogService
            .open(ProjectFormComponent, {
                header: 'Sửa dự án',
                width: '40rem',
                modal: true,
                closable: true,
                data: { project, documentTypes: this._documentTypes },
            })
            ?.onClose.subscribe((changed) => {
                if (!changed) return;
                this.load();
                this.extended.emit();
            });
    }

    private buildColumns(): IColumn<IProjectDeadline>[] {
        const canExtend = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_UPDATE);
        const actions: ITableAction[] = [
            {
                type: DEADLINE_ACTIONS.EXTEND,
                label: 'Gia hạn',
                icon: 'pi pi-calendar-plus',
                visible: (row: IProjectDeadline) => canExtend && row.status !== PROJECT_STATUS.ACCEPTING && row.status !== PROJECT_STATUS.DONE,
            },
            { type: DEADLINE_ACTIONS.VIEW_MAILS, label: 'Xem mail', icon: 'pi pi-envelope' },
        ];
        return [
            { header: 'Dự án', field: 'name', clickable: true, headerContainerStyle: 'min-width: 240px' },
            { header: 'Trạng thái', key: 'status', cellViewType: CellViewTypes.BADGE, badge: (row) => getProjectStatusBadge(row.status) },
            { header: 'Ngày kết thúc', field: 'endedAt', cellViewType: CellViewTypes.DATE, cellStyle: NOWRAP_STYLE },
            { header: 'Còn lại', key: 'daysLeft', cellViewType: CellViewTypes.BADGE, badge: (row) => getDaysLeftBadge(row.daysLeft) },
            {
                header: 'Mail gần nhất',
                field: 'lastNotifiedDate',
                cellViewType: CellViewTypes.CUSTOM_COMP,
                customComponent: DeadlineLastMailCellComponent,
                cellStyle: NOWRAP_STYLE,
            },
            {
                header: 'Thao tác',
                key: 'actions',
                cellViewType: CellViewTypes.ACTIONS,
                frozen: 'right',
                headerContainerStyle: `width: 100px; ${CENTER_STYLE}`,
                cellStyle: CENTER_STYLE,
                actions,
            },
        ];
    }
}
