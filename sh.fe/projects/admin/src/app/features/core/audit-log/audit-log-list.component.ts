import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { IViewAuditLog } from '@/models/audit-log.model';
import { AuditLogService } from '@/services/audit-log.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { AUDIT_LOG_ACTION_TYPES } from '@/shared/constants/audit-log.constants';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { AuditLogDetailComponent } from './audit-log-detail/audit-log-detail.component';

const AUDIT_LOG_ACTIONS = {
    VIEW: 'view',
} as const;

type AuditLogRow = IViewAuditLog & { actionTypeLabel: string; actor: string };

@Component({
    selector: 'app-audit-log-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent],
    templateUrl: './audit-log-list.component.html',
})
export class AuditLogListComponent extends BaseComponent {
    private readonly _auditLogService = inject(AuditLogService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Quản trị' }, { label: 'Nhật ký hệ thống' }];
    readonly actionTypes = AUDIT_LOG_ACTION_TYPES;

    keyword = new FormControl('', { nonNullable: true });
    actionType = new FormControl<string | null>(null);
    logs: AuditLogRow[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = 25;
    override totalRecords = 0;

    readonly columns: IColumn<AuditLogRow>[] = [
        { header: 'Thời gian', field: 'createdDate', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm:ss', headerContainerStyle: 'width: 190px' },
        { header: 'Người thực hiện', field: 'actor' },
        { header: 'Hành động', field: 'action' },
        {
            header: 'Loại',
            field: 'actionTypeLabel',
            cellViewType: CellViewTypes.STATUS,
            headerContainerStyle: 'width: 150px',
            statusSeverity: (row) => this.actionTypes.find((type) => type.value === row.actionType)?.severity ?? 'secondary',
        },
        { header: 'Nguồn', field: 'source' },
        {
            header: '',
            cellViewType: CellViewTypes.ACTIONS,
            headerContainerStyle: 'width: 64px',
            actions: [{ type: AUDIT_LOG_ACTIONS.VIEW, label: 'Xem chi tiết', icon: 'pi pi-eye' }],
        },
    ];

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.search());
        this.load();
    }

    load() {
        const actionType = this.actionType.value;

        this.loading = true;
        this._auditLogService
            .findPaging({ pageNumber: this.pageNumber, pageSize: this.pageSize, keyword: this.keyword.value, ...(actionType ? { actionType } : {}) })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.logs = res.data.items.map((log) => ({
                        ...log,
                        actor: log.fullName || log.userName || '',
                        actionTypeLabel: this.actionTypes.find((type) => type.value === log.actionType)?.label ?? log.actionType,
                    }));
                    this.totalRecords = res.data.totalItems;
                }
            });
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    onAction(event: ITableEvent<IViewAuditLog>) {
        if (event.type === AUDIT_LOG_ACTIONS.VIEW) {
            this._dialogService.open(AuditLogDetailComponent, {
                header: 'Chi tiết nhật ký',
                width: '56rem',
                modal: true,
                closable: true,
                data: { id: event.row!.id },
            });
        }
    }

    search() {
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }
}
