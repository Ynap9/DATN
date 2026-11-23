import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, finalize, forkJoin } from 'rxjs';
import { IViewProjectPair } from '@/models/project.model';
import { ProjectMemberService } from '@/services/project-member.service';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { getProjectStatusBadge, PROJECT_STATUS } from '@/shared/constants/project.constants';
import {
    MEMBER_ERROR_CODE,
    MEMBER_PAGE_SIZE,
    MEMBER_ROLE,
    MEMBER_ROLE_LABELS,
    MEMBER_VIEWER_ADMIN,
} from '@/shared/constants/project-member.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { ProjectMemberAddComponent } from './project-member-add/project-member-add.component';
import {
    IProjectMemberDialogData,
    MEMBER_ACTIONS,
    MemberActionsCellComponent,
    MemberManagerCellComponent,
    MemberNameCellComponent,
    MemberPriceCellComponent,
    MemberRoleCellComponent,
    MemberStatusCellComponent,
    ProjectMemberRow,
} from './project-member-cells';
import { ProjectMemberEditComponent } from './project-member-edit/project-member-edit.component';

const SEARCH_DEBOUNCE_MS = 400;
const RIGHT_STYLE = 'text-align: right';

// Tab lọc vai trò; key '' = Tất cả (không gửi role)
const MEMBER_TABS = [
    { key: '', label: 'Tất cả' },
    { key: MEMBER_ROLE.LEADER, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.LEADER] },
    { key: MEMBER_ROLE.DATA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.DATA] },
    { key: MEMBER_ROLE.QA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.QA] },
];

@Component({
    selector: 'app-project-member-list',
    imports: [SharedImports, DataTable, PageHeader, StatusBadge, ShSelectComponent, IconFieldModule, InputIconModule],
    templateUrl: './project-member-list.component.html',
})
export class ProjectMemberListComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _projectMemberService = inject(ProjectMemberService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Vận hành' }, { label: 'Thành viên dự án' }];
    readonly tabs = MEMBER_TABS;
    readonly pageSize = MEMBER_PAGE_SIZE;
    readonly keyword = new FormControl('', { nonNullable: true });

    // Chỉ các cặp người xem là Admin (màn chỉ dành cho Admin)
    pairs: IViewProjectPair[] = [];
    pairsLoaded = false;
    projects: { projectId: number; projectName: string }[] = [];
    documentTypes: IViewProjectPair[] = [];
    pair: IViewProjectPair | null = null;
    role = '';
    pageNumber = this.START_PAGE_NUMBER;
    rows: ProjectMemberRow[] = [];
    override totalRecords = 0;
    counts: Record<string, number> = {};

    readonly columns: IColumn<ProjectMemberRow>[] = [
        { header: 'Thành viên', key: 'member', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: MemberNameCellComponent },
        { header: 'Vai trò', key: 'role', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: MemberRoleCellComponent },
        { header: 'Leader quản lý', key: 'manager', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: MemberManagerCellComponent },
        {
            header: 'Giá công ty',
            field: 'companyPrice',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: MemberPriceCellComponent,
            headerContainerStyle: RIGHT_STYLE,
            cellStyle: RIGHT_STYLE,
        },
        {
            header: 'Giá thực nhận',
            field: 'actualPrice',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: MemberPriceCellComponent,
            headerContainerStyle: RIGHT_STYLE,
            cellStyle: RIGHT_STYLE,
        },
        { header: 'Trạng thái', key: 'status', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: MemberStatusCellComponent },
        {
            header: 'Thao tác',
            key: 'actions',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: MemberActionsCellComponent,
            headerContainerStyle: 'width: 140px; text-align: center',
        },
    ];

    // Dự án đã xong / hết hạn: chỉ xem (BE chặn 818 cả SuperAdmin)
    get isClosed() {
        return this.pair?.status === PROJECT_STATUS.DONE || this.pair?.status === PROJECT_STATUS.EXPIRED;
    }

    get statusBadge() {
        return getProjectStatusBadge(this.pair!.status);
    }

    override ngOnInit(): void {
        this.keyword.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef)).subscribe(() => {
            this.pageNumber = this.START_PAGE_NUMBER;
            this.load();
        });
        this.loadPairs(() => {
            this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applySelection());
        });
    }

    // Lấy hết cặp dự án – loại tài liệu một lần, giữ cặp Admin; dự án khác nhau sắp theo tên
    loadPairs(onLoaded?: () => void) {
        this._projectService
            .findPaging({ pageNumber: 1, pageSize: -1 })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                this.pairs = res.data.items.filter((item) => item.viewerRole === MEMBER_VIEWER_ADMIN);
                const projectNames = new Map(this.pairs.map((item) => [item.projectId, item.projectName]));
                this.projects = [...projectNames]
                    .map(([projectId, projectName]) => ({ projectId, projectName }))
                    .sort((a, b) => a.projectName.localeCompare(b.projectName, 'vi'));
                this.pairsLoaded = true;
                if (onLoaded) {
                    onLoaded();
                } else {
                    this.applySelection();
                }
            });
    }

    // Đọc cặp từ URL; thiếu / không còn trong danh sách thì ghi cặp đầu tiên lên URL (queryParamMap gọi lại hàm này)
    applySelection() {
        const params = this._activatedRoute.snapshot.queryParamMap;
        const projectId = Number(params.get('projectId'));
        const documentTypeId = Number(params.get('documentTypeId'));
        const found = this.pairs.find((item) => item.projectId === projectId && item.documentTypeId === documentTypeId);
        if (!found) {
            this.pair = null;
            this.rows = [];
            if (this.pairs.length) {
                const first = this.pairs.find((item) => item.projectId === this.projects[0].projectId)!;
                this.selectPair(first.projectId, first.documentTypeId);
            }
            return;
        }
        const isPairChanged = found.projectId !== this.pair?.projectId || found.documentTypeId !== this.pair?.documentTypeId;
        this.pair = found;
        this.documentTypes = this.pairs.filter((item) => item.projectId === found.projectId);
        if (isPairChanged) {
            this.role = '';
            this.pageNumber = this.START_PAGE_NUMBER;
        }
        this.load();
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với cặp đang chọn; đổi dự án thì lấy loại tài liệu đầu tiên
    onProjectChange(projectId: number) {
        if (projectId === this.pair?.projectId) return;
        const first = this.pairs.find((item) => item.projectId === projectId);
        if (first) {
            this.selectPair(projectId, first.documentTypeId);
        }
    }

    onDocumentTypeChange(documentTypeId: number) {
        if (documentTypeId === this.pair?.documentTypeId) return;
        this.selectPair(this.pair!.projectId, documentTypeId);
    }

    selectTab(role: string) {
        if (role === this.role) return;
        this.role = role;
        this.pageNumber = this.START_PAGE_NUMBER;
        this.loadPage();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.loadPage();
    }

    load() {
        this.loadCounts();
        this.loadPage();
    }

    // Số trên tab = totalItems của 4 lần paging pageSize 1, cùng từ khoá
    loadCounts() {
        if (!this.pair) return;
        const { projectId, documentTypeId } = this.pair;
        const keyword = this.keyword.value.trim();
        forkJoin(
            this.tabs.map((tab) =>
                this._projectMemberService.findPaging(projectId, { documentTypeId, role: tab.key, keyword, pageNumber: 1, pageSize: 1 }),
            ),
        )
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((responses) => {
                if (responses.every((res) => this.isResponseSucceed(res, false))) {
                    this.counts = Object.fromEntries(this.tabs.map((tab, index) => [tab.key, responses[index].data.totalItems]));
                }
            });
    }

    loadPage() {
        if (!this.pair) return;
        const { projectId, documentTypeId } = this.pair;
        this.loading = true;
        this._projectMemberService
            .findPaging(projectId, {
                documentTypeId,
                role: this.role,
                keyword: this.keyword.value.trim(),
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
            })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this.handleMemberError(res.code);
                this.rows = res.data.items.map((member) => ({ ...member, canEdit: !this.isClosed }));
                this.totalRecords = res.data.totalItems;
            });
    }

    onAction(event: ITableEvent<ProjectMemberRow>) {
        if (event.type === MEMBER_ACTIONS.EDIT) {
            this.openEditForm(event.row!);
        } else if (event.type === MEMBER_ACTIONS.REMOVE) {
            this.confirmRemove(event.row!);
        }
    }

    openAddForm() {
        const data: IProjectMemberDialogData = { pair: this.pair!, onMemberError: (code) => this.handleMemberError(code) };
        this._dialogService
            .open(ProjectMemberAddComponent, { header: 'Thêm thành viên', width: '520px', modal: true, closable: true, data })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    // 818 dự án vừa đóng / 805 vừa mất quyền → tải lại danh sách cặp; 806 thành viên không còn → tải lại bảng
    handleMemberError(code: number) {
        if (code === MEMBER_ERROR_CODE.OPERATION_ERROR_PROJECT_CLOSED || code === MEMBER_ERROR_CODE.OPERATION_ERROR_NOT_PROJECT_LEADER) {
            this.loadPairs();
        } else if (code === MEMBER_ERROR_CODE.OPERATION_ERROR_MEMBER_NOT_FOUND) {
            this.load();
        }
    }

    private openEditForm(member: ProjectMemberRow) {
        const data: IProjectMemberDialogData = { pair: this.pair!, member, onMemberError: (code) => this.handleMemberError(code) };
        this._dialogService
            .open(ProjectMemberEditComponent, { header: 'Sửa thành viên', width: '480px', modal: true, closable: true, data })
            ?.onClose.subscribe((changed) => changed && this.loadPage());
    }

    private confirmRemove(member: ProjectMemberRow) {
        const name = member.fullName || member.userName;
        const isLeader = member.role === MEMBER_ROLE.LEADER;
        this._confirmationService.confirm({
            header: `Gỡ ${name}?`,
            message: isLeader
                ? 'Người do Leader này quản lý sẽ chuyển sang Admin quản lý.'
                : `Đang giữ ${member.heldTicketCount} phiếu. Văn bản chưa kiểm / đang bị trả sẽ về “chưa giao”; văn bản đã kiểm vẫn giữ tên và vẫn tính tiền.`,
            closable: true,
            closeOnEscape: true,
            icon: 'pi pi-exclamation-triangle',
            rejectButtonProps: { label: 'Huỷ', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Gỡ thành viên', severity: 'danger' },
            accept: () => {
                this._projectMemberService
                    .remove(member.projectId, member.id)
                    .pipe(takeUntilDestroyed(this._destroyRef))
                    .subscribe((res) => {
                        if (!this.isResponseSucceed(res)) return this.handleMemberError(res.code);
                        this.messageSuccess(
                            isLeader
                                ? `Đã gỡ ${name}, ${res.data.unmanagedMemberCount} người chuyển sang Admin quản lý`
                                : `Đã gỡ ${name}, thu hồi ${res.data.revokedDocumentCount} văn bản`,
                        );
                        this.load();
                    });
            },
        });
    }

    private selectPair(projectId: number, documentTypeId: number) {
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams: { projectId, documentTypeId }, replaceUrl: true });
    }
}
