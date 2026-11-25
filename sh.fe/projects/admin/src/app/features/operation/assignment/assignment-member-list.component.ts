import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, finalize } from 'rxjs';
import { IViewProjectPair } from '@/models/project.model';
import { ProjectMemberService } from '@/services/project-member.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { ProjectPairPicker } from '@/shared/components/project-pair-picker/project-pair-picker.component';
import { ASSIGNMENT_MEMBER_PAGE_SIZE, ASSIGNMENT_ROLE, AssignmentRole } from '@/shared/constants/assignment.constants';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUS } from '@/shared/constants/project.constants';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS, MEMBER_VIEWER_ADMIN } from '@/shared/constants/project-member.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import {
    ASSIGN_DIALOG_RESULT,
    ASSIGNMENT_ACTIONS,
    AssignmentActionsCellComponent,
    AssignmentMemberRow,
    AssignmentNameCellComponent,
    AssignmentStatusCellComponent,
    IAssignDialogData,
    ITicketDialogData,
    TICKET_DIALOG_RESULT,
} from './assignment-cells';
import { AssignDialogComponent } from './assign-dialog/assign-dialog.component';
import { TicketDialogComponent } from './ticket-dialog/ticket-dialog.component';

const SEARCH_DEBOUNCE_MS = 400;
const RIGHT_STYLE = 'text-align: right';

// Lọc trạng thái trên URL: mặc định chỉ người đang hoạt động (không ghi lên URL)
const ACTIVE_FILTER = { ACTIVE: 'true', INACTIVE: 'false', ALL: 'all' } as const;
const ACTIVE_OPTIONS = [
    { value: ACTIVE_FILTER.ACTIVE, label: 'Đang hoạt động' },
    { value: ACTIVE_FILTER.INACTIVE, label: 'Đã tắt' },
    { value: ACTIVE_FILTER.ALL, label: 'Mọi trạng thái' },
];

const GUIDES: Record<AssignmentRole, string> = {
    [ASSIGNMENT_ROLE.DATA]: 'Bấm Chia phiếu ở dòng người nhập liệu, tích hộp / hồ sơ cần giao. Tích hộp = giao mọi hồ sơ trong hộp. Mỗi lần giao tạo một phiếu.',
    [ASSIGNMENT_ROLE.QA]: 'Bấm Chia phiếu ở dòng QA để giao phụ trách hộp / hồ sơ. Văn bản đã kiểm vào QA ngay, văn bản còn đang nhập kiểm xong tự về QA đó.',
};

@Component({
    selector: 'app-assignment-member-list',
    imports: [SharedImports, DataTable, PageHeader, ProjectPairPicker, ShSelectComponent, IconFieldModule, InputIconModule],
    templateUrl: './assignment-member-list.component.html',
})
export class AssignmentMemberListComponent extends BaseComponent {
    private readonly _projectMemberService = inject(ProjectMemberService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _picker = viewChild(ProjectPairPicker);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Vận hành' }, { label: 'Chia phiếu' }];
    // Admin mọi cặp; Leader cặp mình làm Leader (BE chỉ trả người Leader đó quản lý)
    readonly viewerRoles = [MEMBER_VIEWER_ADMIN, MEMBER_ROLE.LEADER];
    readonly activeOptions = ACTIVE_OPTIONS;
    readonly pageSize = ASSIGNMENT_MEMBER_PAGE_SIZE;
    readonly keyword = new FormControl('', { nonNullable: true });
    // Bảng người gọi API thành viên dự án: thiếu quyền này thì không gọi
    readonly canViewMembers = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE);

    pairs: IViewProjectPair[] | null = null;
    pair: IViewProjectPair | null = null;
    role: AssignmentRole = ASSIGNMENT_ROLE.DATA;
    activeFilter: string = ACTIVE_FILTER.ACTIVE;
    pageNumber = this.START_PAGE_NUMBER;
    rows: AssignmentMemberRow[] = [];
    override totalRecords = 0;
    // Cặp + lọc của lần tải gần nhất: picker và URL cùng báo đổi thì chỉ tải một lần
    private _loadedKey = '';

    readonly columns: IColumn<AssignmentMemberRow>[] = [
        { header: 'Tài khoản', field: 'userName' },
        { header: 'Họ tên', key: 'fullName', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: AssignmentNameCellComponent },
        { header: 'Người quản lý', field: 'managedByFullName' },
        { header: 'Phiếu đang làm', field: 'heldTicketCount', headerContainerStyle: RIGHT_STYLE, cellStyle: RIGHT_STYLE },
        { header: 'Trạng thái', key: 'status', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: AssignmentStatusCellComponent },
        {
            header: 'Thao tác',
            key: 'actions',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: AssignmentActionsCellComponent,
            headerContainerStyle: 'width: 200px; text-align: center',
        },
    ];

    get roleTabs() {
        const tabs = [{ key: ASSIGNMENT_ROLE.DATA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.DATA] }];
        return this.pair?.isQaEnabled ? [...tabs, { key: ASSIGNMENT_ROLE.QA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.QA] }] : tabs;
    }

    get guide() {
        return GUIDES[this.role];
    }

    // Dự án đã xong / hết hạn: chỉ xem (BE chặn 818)
    get isClosed() {
        return this.pair?.status === PROJECT_STATUS.DONE || this.pair?.status === PROJECT_STATUS.EXPIRED;
    }

    get isLeaderView() {
        return this.pair?.viewerRole === MEMBER_ROLE.LEADER;
    }

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef))
            .subscribe((keyword) => this.navigateFilters({ keyword: keyword.trim() }));
        this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.refresh(false));
    }

    // Picker phát cặp mới hoặc phát lại cặp cũ sau reload (dữ liệu dự án có thể đã đổi) → luôn tải lại
    onPairChange(pair: IViewProjectPair | null) {
        this.pair = pair;
        this.refresh(true);
    }

    selectTab(role: AssignmentRole) {
        if (role === this.role) return;
        this.navigateFilters({ role: role === ASSIGNMENT_ROLE.DATA ? '' : role });
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với giá trị đang lọc
    onActiveChange(value: string | null) {
        const activeFilter = value || ACTIVE_FILTER.ACTIVE;
        if (activeFilter === this.activeFilter) return;
        this.navigateFilters({ isActive: activeFilter === ACTIVE_FILTER.ACTIVE ? '' : activeFilter });
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.loadPage();
    }

    onAction(event: ITableEvent<AssignmentMemberRow>) {
        const member = event.row!;
        if (event.type === ASSIGNMENT_ACTIONS.TICKETS) {
            const ticketData: ITicketDialogData = { pair: this.pair!, member, role: this.role, isClosed: this.isClosed };
            this._dialogService
                .open(TicketDialogComponent, { width: '860px', modal: true, showHeader: false, closable: false, closeOnEscape: false, data: ticketData })
                ?.onClose.subscribe((result) => {
                    if (result === TICKET_DIALOG_RESULT.RELOAD_PAIRS) {
                        this._picker()?.reload();
                    } else if (result) {
                        this.loadPage();
                    }
                });
            return;
        }
        const data: IAssignDialogData = { pair: this.pair!, member, role: this.role };
        this._dialogService
            .open(AssignDialogComponent, { width: '860px', modal: true, showHeader: false, closable: false, closeOnEscape: false, data })
            ?.onClose.subscribe((result) => {
                if (result === ASSIGN_DIALOG_RESULT.RELOAD_PAIRS) {
                    this._picker()?.reload();
                } else if (result) {
                    this.loadPage();
                }
            });
    }

    // Lọc nằm trên URL (?role=&isActive=&keyword=); đọc lại rồi tải nếu cặp / lọc khác lần trước (đổi lọc → trang 1)
    private refresh(isForced: boolean) {
        const params = this._activatedRoute.snapshot.queryParamMap;
        const role = params.get('role');
        this.role = role === ASSIGNMENT_ROLE.QA && this.pair?.isQaEnabled ? ASSIGNMENT_ROLE.QA : ASSIGNMENT_ROLE.DATA;
        const activeFilter = params.get('isActive');
        this.activeFilter = activeFilter === ACTIVE_FILTER.INACTIVE || activeFilter === ACTIVE_FILTER.ALL ? activeFilter : ACTIVE_FILTER.ACTIVE;
        const keyword = params.get('keyword') ?? '';
        if (this.keyword.value.trim() !== keyword) {
            this.keyword.setValue(keyword, { emitEvent: false });
        }

        if (!this.pair || !this.canViewMembers) {
            this.rows = [];
            this.totalRecords = 0;
            this._loadedKey = '';
            return;
        }
        const key = [this.pair.projectId, this.pair.documentTypeId, this.role, this.activeFilter, keyword].join('|');
        if (key === this._loadedKey && !isForced) return;
        if (key !== this._loadedKey) {
            this.pageNumber = this.START_PAGE_NUMBER;
        }
        this._loadedKey = key;
        this.loadPage();
    }

    private loadPage() {
        if (!this.pair) return;
        const { projectId, documentTypeId } = this.pair;
        this.loading = true;
        this._projectMemberService
            .findPaging(projectId, {
                documentTypeId,
                role: this.role,
                isActive: this.activeFilter === ACTIVE_FILTER.ALL ? undefined : this.activeFilter === ACTIVE_FILTER.ACTIVE,
                keyword: this.keyword.value.trim(),
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
            })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                this.rows = res.data.items.map((member) => ({
                    ...member,
                    assignDisabledReason: this.isClosed ? 'Dự án đã xong / hết hạn' : member.isActive ? '' : 'Thành viên đã tắt',
                }));
                this.totalRecords = res.data.totalItems;
            });
    }

    private navigateFilters(change: { role?: string; isActive?: string; keyword?: string }) {
        const queryParams = Object.fromEntries(Object.entries(change).map(([key, value]) => [key, value || null]));
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
    }
}
