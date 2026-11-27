import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { debounceTime, finalize } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IFindPagingProject, IViewProject, IViewProjectPair } from '@/models/project.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { getProjectStatusBadge, PROJECT_STATUSES, PROJECT_URL } from '@/shared/constants/project.constants';
import { SUBMISSION_ROLE, SUBMISSION_ROLE_BY_VIEWER_ROLE } from '@/shared/constants/submission.constants';
import { IColumn, IColumnFilterValue, IDateRangeValue, ITableAction, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { ProjectCardComponent } from './project-card/project-card.component';
import { ProjectDeleteDialogComponent } from './project-delete-dialog/project-delete-dialog.component';
import { ProjectFormComponent } from './project-form/project-form.component';

const PROJECT_ACTIONS = {
    REPORT: 'report',
    OPEN_SHEET: 'openSheet',
    EDIT: 'edit',
    DELETE: 'delete',
} as const;

const PROGRESS_TOOLTIPS: Record<string, string> = {
    Data: 'Bạn đã kiểm / văn bản bạn giữ',
    QA: 'Bạn đã chấm / văn bản bạn phụ trách',
};

const PROJECT_VIEW_MODES = [
    { value: 'table', icon: 'pi pi-list', label: 'Dạng bảng' },
    { value: 'card', icon: 'pi pi-th-large', label: 'Dạng thẻ' },
] as const;
const VIEW_MODE_STORAGE_KEY = 'construction-project-view';
const CARD_SKELETON_COUNT = 6;

const NUMBER_STYLE = 'text-align: right';
const CENTER_STYLE = 'text-align: center';

type ProjectViewMode = (typeof PROJECT_VIEW_MODES)[number]['value'];
type ProjectListFilters = Omit<IFindPagingProject, 'pageNumber' | 'pageSize'>;

@Component({
    selector: 'app-project-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent, PaginatorModule, SkeletonModule, ProjectCardComponent, ProjectDeleteDialogComponent],
    templateUrl: './project-list.component.html',
    styleUrl: './project-list.component.scss',
})
export class ProjectListComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _isAdmin = this._sharedService.isSuperAdmin() || this._sharedService.roles.includes(AuthConstants.ADMIN_ROLE);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Hồ sơ thi công' }, { label: 'Dự án' }];
    readonly canCreate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CREATE);
    readonly statusTabs = [{ value: undefined, label: 'Tất cả', tone: undefined }, ...PROJECT_STATUSES];
    readonly viewModes = PROJECT_VIEW_MODES;
    readonly rowsPerPageOptions = [10, 25, 50, 100];
    readonly skeletons = Array.from({ length: CARD_SKELETON_COUNT }, (_, index) => index);
    // Bảng và thẻ dùng chung một menu ⋯; quyền tính một lần khi mở trang
    readonly rowActions: ITableAction[] = [
        // Báo cáo tiến độ chỉ dành cho quản lý dự án (Admin / Leader của metadata trên dòng)
        {
            type: PROJECT_ACTIONS.REPORT,
            label: 'Báo cáo tiến độ',
            icon: 'pi pi-chart-bar',
            visible: (row: IViewProjectPair) => SUBMISSION_ROLE_BY_VIEWER_ROLE[row.viewerRole] === SUBMISSION_ROLE.MANAGER,
        },
        {
            type: PROJECT_ACTIONS.OPEN_SHEET,
            label: 'Mở Google Sheet',
            icon: 'pi pi-external-link',
            visible: (row: IViewProjectPair) => !!row.urlGoogleSheetSync,
        },
        ...(this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_UPDATE)
            ? [{ type: PROJECT_ACTIONS.EDIT, label: 'Sửa', icon: 'pi pi-pencil' }]
            : []),
        ...(this.canDelete ? [{ type: PROJECT_ACTIONS.DELETE, label: 'Xóa', icon: 'pi pi-trash', severity: 'danger' as const }] : []),
    ];

    viewMode: ProjectViewMode = this.readViewMode();
    keyword = new FormControl('', { nonNullable: true });
    documentTypeId = new FormControl<number | null>(null);
    // Một nguồn lọc duy nhất: tab, ô tìm, ô metadata và popover cột cùng đọc / ghi
    filters: ProjectListFilters = {};
    columnFilters: IColumnFilterValue = {};
    documentTypes: IDocumentTypeDropdown[] = [];
    projects: IViewProjectPair[] = [];
    deleteTarget: IViewProjectPair | null = null;
    deleteDialogVisible = false;
    columns: IColumn<IViewProjectPair>[] = this.buildColumns();
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = 25;
    override totalRecords = 0;

    get activeFilterCount() {
        return Object.keys(this.columnFilters).filter((key) => key !== 'status').length;
    }

    get canDelete() {
        return this._isAdmin && this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_DELETE);
    }

    override ngOnInit(): void {
        // So với state thay vì distinctUntilChanged: popover cột có thể đổi keyword mà ô tìm không phát sự kiện
        this.keyword.valueChanges.pipe(debounceTime(400), takeUntilDestroyed(this._destroyRef)).subscribe((value) => {
            const keyword = value.trim() || undefined;
            if (keyword !== this.filters.keyword) {
                this.setFilters({ keyword });
            }
        });
        // sh-select phát null lúc khởi tạo: so với state để không tải trùng lần đầu
        this.documentTypeId.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((value) => {
            const documentTypeId = value ?? undefined;
            if (documentTypeId !== this.filters.documentTypeId) {
                this.setFilters({ documentTypeId });
            }
        });
        this._documentTypeService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.documentTypes = res.data;
                    this.columns = this.buildColumns();
                }
            });
        this.load();
    }

    load() {
        this.loading = true;
        this._projectService
            .findPaging({ pageNumber: this.pageNumber, pageSize: this.pageSize, ...this.filters })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.projects = res.data.items;
                    this.totalRecords = res.data.totalItems;
                }
            });
    }

    setViewMode(viewMode: ProjectViewMode) {
        this.viewMode = viewMode;
        try {
            localStorage.setItem(VIEW_MODE_STORAGE_KEY, viewMode);
        } catch {
            // localStorage bị chặn: chỉ nhớ dạng xem tới khi tải lại trang
        }
    }

    // Dòng là cặp dự án + metadata nên mở thẳng danh sách văn bản của metadata đó
    openProject(project: IViewProjectPair) {
        this.router.navigate([PROJECT_URL, project.projectId, 'metadata', project.documentTypeId]);
    }

    openReport(project: IViewProjectPair) {
        this.router.navigate([PROJECT_URL, project.projectId]);
    }

    selectStatus(status: number | undefined) {
        this.setFilters({ status });
    }

    onColumnFilterChange(values: IColumnFilterValue) {
        const started = values['startedAt'] as IDateRangeValue | undefined;
        const ended = values['endedAt'] as IDateRangeValue | undefined;
        this.setFilters({
            keyword: values['projectName'] as string | undefined,
            documentTypeId: values['documentTypeName'] as number | undefined,
            creatorKeyword: values['creatorName'] as string | undefined,
            startedFrom: started?.from,
            startedTo: started?.to,
            endedFrom: ended?.from,
            endedTo: ended?.to,
            status: values['status'] as number | undefined,
        });
    }

    clearFilters() {
        this.filters = { status: this.filters.status };
        this.applyFilters();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    onAction(event: ITableEvent<IViewProjectPair>) {
        if (event.type === TableEventTypes.CELL_CLICK) {
            this.openProject(event.row!);
        } else if (event.type === PROJECT_ACTIONS.REPORT) {
            this.openReport(event.row!);
        } else if (event.type === PROJECT_ACTIONS.OPEN_SHEET) {
            window.open(event.row!.urlGoogleSheetSync!, '_blank', 'noopener');
        } else if (event.type === PROJECT_ACTIONS.EDIT) {
            this.openEditForm(event.row!.projectId);
        } else if (event.type === PROJECT_ACTIONS.DELETE) {
            this.confirmDeleteProject(event.row!);
        }
    }

    openForm(project?: IViewProject) {
        this._dialogService
            .open(ProjectFormComponent, {
                header: project ? 'Sửa dự án' : 'Tạo dự án',
                width: '40rem',
                modal: true,
                closable: true,
                data: { project, documentTypes: this.documentTypes },
            })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    private setFilters(patch: ProjectListFilters) {
        this.filters = { ...this.filters, ...patch };
        this.applyFilters();
    }

    // Đồng bộ state lọc ra ô tìm, ô metadata, popover cột rồi tải lại từ trang 1
    private applyFilters() {
        const filters = this.filters;
        this.keyword.setValue(filters.keyword ?? '', { emitEvent: false });
        this.documentTypeId.setValue(filters.documentTypeId ?? null, { emitEvent: false });
        const values: IColumnFilterValue = {
            projectName: filters.keyword,
            documentTypeName: filters.documentTypeId,
            creatorName: filters.creatorKeyword,
            startedAt: filters.startedFrom || filters.startedTo ? { from: filters.startedFrom, to: filters.startedTo } : undefined,
            endedAt: filters.endedFrom || filters.endedTo ? { from: filters.endedFrom, to: filters.endedTo } : undefined,
            status: filters.status,
        };
        this.columnFilters = Object.fromEntries(Object.entries(values).filter(([, value]) => value !== undefined));
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }

    private buildColumns(): IColumn<IViewProjectPair>[] {
        return [
            {
                header: 'STT',
                key: 'index',
                cellViewType: CellViewTypes.INDEX,
                locked: true,
                frozen: 'left',
                headerContainerStyle: 'width: 64px',
            },
            {
                header: 'Tên dự án',
                field: 'projectName',
                clickable: true,
                locked: true,
                frozen: 'left',
                cellClass: 'project-name-cell',
                headerContainerStyle: 'min-width: 240px',
                filter: { type: 'text' },
            },
            {
                header: 'Metadata',
                field: 'documentTypeName',
                headerContainerStyle: 'min-width: 160px',
                filter: { type: 'select', options: this.documentTypes.map((type) => ({ label: type.name, value: type.id })) },
            },
            ...(this._isAdmin
                ? []
                : [
                      {
                          header: 'Vai trò',
                          field: 'viewerRole',
                          cellViewType: CellViewTypes.STATUS,
                          statusSeverity: () => 'secondary' as const,
                      },
                  ]),
            {
                header: 'Số văn bản',
                field: 'totalSubmission',
                cellViewType: CellViewTypes.NUMBER,
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Số trang',
                field: 'totalPages',
                cellViewType: CellViewTypes.NUMBER,
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Đã OCR',
                field: 'ocrDoneCount',
                cellViewType: CellViewTypes.NUMBER,
                cellClass: 'project-ocr-done',
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Chưa OCR',
                field: 'ocrPendingCount',
                cellViewType: CellViewTypes.NUMBER,
                cellClass: 'project-ocr-pending',
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Tiến độ xác nhận',
                field: 'progressPercent',
                cellViewType: CellViewTypes.PROGRESS,
                progressTooltip: (row) =>
                    `${PROGRESS_TOOLTIPS[row.viewerRole] ?? 'Đã kiểm / tổng văn bản'} · ${row.checkedCount} / ${row.totalSubmission}`,
            },
            {
                header: 'QA đạt',
                field: 'qaPassedCount',
                cellViewType: CellViewTypes.NUMBER,
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Thành viên',
                field: 'memberCount',
                cellViewType: CellViewTypes.NUMBER,
                headerContainerStyle: NUMBER_STYLE,
                cellStyle: NUMBER_STYLE,
            },
            {
                header: 'Ngày bắt đầu',
                field: 'startedAt',
                cellViewType: CellViewTypes.DATE,
                headerContainerStyle: CENTER_STYLE,
                cellStyle: CENTER_STYLE,
                filter: { type: 'dateRange' },
            },
            {
                header: 'Ngày kết thúc',
                field: 'endedAt',
                cellViewType: CellViewTypes.DATE,
                headerContainerStyle: CENTER_STYLE,
                cellStyle: CENTER_STYLE,
                filter: { type: 'dateRange' },
            },
            { header: 'Người tạo', field: 'creatorName', filter: { type: 'text' } },
            {
                header: 'Trạng thái',
                field: 'status',
                cellViewType: CellViewTypes.BADGE,
                headerContainerStyle: CENTER_STYLE,
                cellStyle: CENTER_STYLE,
                badge: (row) => getProjectStatusBadge(row.status),
                filter: { type: 'select', options: PROJECT_STATUSES.map((status) => ({ label: status.label, value: status.value })) },
            },
            {
                header: 'Thao tác',
                key: 'actions',
                cellViewType: CellViewTypes.ACTIONS,
                locked: true,
                frozen: 'right',
                headerContainerStyle: 'width: 56px; text-align: center',
                cellStyle: CENTER_STYLE,
                actions: this.rowActions,
            },
        ];
    }

    private readViewMode(): ProjectViewMode {
        try {
            return localStorage.getItem(VIEW_MODE_STORAGE_KEY) === 'card' ? 'card' : 'table';
        } catch {
            return 'table';
        }
    }

    // Một dòng danh sách chỉ mang một metadata, cần GET {id} để có đủ documentTypeIds của dự án
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

    private confirmDeleteProject(project: IViewProjectPair) {
        this.deleteTarget = project;
        this.deleteDialogVisible = true;
    }
}
