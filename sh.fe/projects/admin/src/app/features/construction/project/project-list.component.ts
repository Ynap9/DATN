import { formatDate } from '@angular/common';
import { Component, DestroyRef, inject, LOCALE_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { SelectButtonModule } from 'primeng/selectbutton';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IViewProject, IViewProjectPair } from '@/models/project.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUSES, PROJECT_URL } from '@/shared/constants/project.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { ProjectFormComponent } from './project-form/project-form.component';
import { ProjectProgressCellComponent } from './project-progress-cell/project-progress-cell.component';

const PROJECT_ACTIONS = {
    VIEW: 'view',
    OPEN_SHEET: 'openSheet',
    EDIT: 'edit',
    DELETE: 'delete',
} as const;

type ProjectPairRow = IViewProjectPair & { periodLabel: string; statusLabel?: string };

@Component({
    selector: 'app-project-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent, SelectButtonModule],
    templateUrl: './project-list.component.html',
})
export class ProjectListComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _locale = inject(LOCALE_ID);
    private readonly _isAdmin = this._sharedService.isSuperAdmin() || this._sharedService.roles.includes(AuthConstants.ADMIN_ROLE);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Hồ sơ thi công' }, { label: 'Dự án' }];
    readonly canCreate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CREATE);
    readonly statusTabs = [{ value: null, label: 'Tất cả' }, ...PROJECT_STATUSES];

    keyword = new FormControl('', { nonNullable: true });
    status = new FormControl<number | null>(null);
    documentTypeId = new FormControl<number | null>(null);
    documentTypes: IDocumentTypeDropdown[] = [];
    projects: ProjectPairRow[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = 25;
    override totalRecords = 0;

    readonly columns: IColumn<ProjectPairRow>[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 64px' },
        { header: 'Tên dự án', field: 'projectName', clickable: true },
        { header: 'Metadata', field: 'documentTypeName' },
        ...(this._isAdmin
            ? []
            : [{ header: 'Vai trò', field: 'viewerRole', cellViewType: CellViewTypes.STATUS, statusSeverity: () => 'secondary' as const }]),
        { header: 'Thời gian', field: 'periodLabel', headerContainerStyle: 'width: 190px' },
        { header: 'Văn bản', field: 'totalSubmission', headerContainerStyle: 'width: 90px' },
        { header: 'Tiến độ', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: ProjectProgressCellComponent, headerContainerStyle: 'width: 170px' },
        { header: 'QA đạt', field: 'qaPassedCount', headerContainerStyle: 'width: 80px' },
        { header: 'Thành viên', field: 'memberCount', headerContainerStyle: 'width: 100px' },
        {
            header: 'Trạng thái',
            field: 'statusLabel',
            cellViewType: CellViewTypes.STATUS,
            statusSeverity: (row) => PROJECT_STATUSES.find((status) => status.value === row.status)?.severity ?? 'secondary',
            headerContainerStyle: 'width: 150px',
        },
        {
            header: 'Thao tác',
            cellViewType: CellViewTypes.ACTIONS,
            headerContainerStyle: 'width: 90px; text-align: center',
            actions: [
                { type: PROJECT_ACTIONS.VIEW, label: 'Xem chi tiết', icon: 'pi pi-eye' },
                {
                    type: PROJECT_ACTIONS.OPEN_SHEET,
                    label: 'Mở Google Sheet',
                    icon: 'pi pi-external-link',
                    visible: (row: ProjectPairRow) => !!row.urlGoogleSheetSync,
                },
                ...(this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_UPDATE)
                    ? [{ type: PROJECT_ACTIONS.EDIT, label: 'Sửa', icon: 'pi pi-pencil' }]
                    : []),
                ...(this.canDelete ? [{ type: PROJECT_ACTIONS.DELETE, label: 'Xóa', icon: 'pi pi-trash', severity: 'danger' as const }] : []),
            ],
        },
    ];

    get canDelete() {
        return this._isAdmin && this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_DELETE);
    }

    get isFiltering() {
        return !!this.keyword.value || this.status.value !== null || this.documentTypeId.value !== null;
    }

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.search());
        this.status.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.search());
        this.documentTypeId.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.search());
        this._documentTypeService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.documentTypes = res.data;
                }
            });
        this.load();
    }

    load() {
        this.loading = true;
        this._projectService
            .findPaging({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                keyword: this.keyword.value,
                ...(this.status.value !== null ? { status: this.status.value } : {}),
                ...(this.documentTypeId.value !== null ? { documentTypeId: this.documentTypeId.value } : {}),
            })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.projects = res.data.items.map((project) => ({
                        ...project,
                        periodLabel: project.startedAt || project.endedAt ? `${this.formatDay(project.startedAt)} – ${this.formatDay(project.endedAt)}` : '—',
                        statusLabel: PROJECT_STATUSES.find((status) => status.value === project.status)?.label,
                    }));
                    this.totalRecords = res.data.totalItems;
                }
            });
    }

    search() {
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    onAction(event: ITableEvent<ProjectPairRow>) {
        if (event.type === TableEventTypes.CELL_CLICK || event.type === PROJECT_ACTIONS.VIEW) {
            this.router.navigate([PROJECT_URL, event.row!.projectId]);
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

    private confirmDeleteProject(project: ProjectPairRow) {
        this.confirmDelete(
            {
                header: 'Xóa dự án',
                message: `Xóa dự án ${project.projectName} cùng toàn bộ văn bản, phiếu, thành viên, cấu hình AI? Không khôi phục được.`,
            },
            () => {
                this._projectService
                    .deleteCascade(project.projectId)
                    .pipe(takeUntilDestroyed(this._destroyRef))
                    .subscribe((res) => {
                        if (this.isResponseSucceed(res, true, 'Xóa dự án thành công')) {
                            this.load();
                        }
                    });
            },
        );
    }

    private formatDay(date: string | null) {
        return date ? formatDate(date, 'dd/MM/yyyy', this._locale) : '—';
    }
}
