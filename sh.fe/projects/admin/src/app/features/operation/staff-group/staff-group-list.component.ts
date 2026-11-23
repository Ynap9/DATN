import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, finalize } from 'rxjs';
import { IViewStaffGroup } from '@/models/staff-group.model';
import { SharedService } from '@/services/shared.service';
import { StaffGroupService } from '@/services/staff-group.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';
import { STAFF_GROUP_PAGE_SIZE } from '@/shared/constants/staff-group.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import {
    IStaffGroupDialogData,
    STAFF_GROUP_ACTIONS,
    StaffGroupActionsCellComponent,
    StaffGroupNameCellComponent,
    StaffGroupRoleCellComponent,
} from './staff-group-cells';
import { StaffGroupFormComponent } from './staff-group-form/staff-group-form.component';

const SEARCH_DEBOUNCE_MS = 400;
// Giá trị ô Trưởng nhóm = không lọc (không ghi lên URL)
const ALL_OWNERS = 'all';

// Tab lọc vai trò; key '' = Tất cả (không gửi role)
const STAFF_GROUP_TABS = [
    { key: '', label: 'Tất cả' },
    { key: MEMBER_ROLE.DATA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.DATA] },
    { key: MEMBER_ROLE.QA, label: MEMBER_ROLE_LABELS[MEMBER_ROLE.QA] },
];

@Component({
    selector: 'app-staff-group-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent, IconFieldModule, InputIconModule],
    templateUrl: './staff-group-list.component.html',
})
export class StaffGroupListComponent extends BaseComponent {
    private readonly _staffGroupService = inject(StaffGroupService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Vận hành' }, { label: 'Nhóm nhân viên' }];
    readonly tabs = STAFF_GROUP_TABS;
    readonly pageSize = STAFF_GROUP_PAGE_SIZE;
    readonly keyword = new FormControl('', { nonNullable: true });
    // Mọi API nhóm cần quyền này; chỉ có quyền menu thì không gọi API
    readonly canManage = this._sharedService.isGranted(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE);
    // Leader bị BE ép về nhóm của mình nên không có cột / ô Trưởng nhóm
    readonly isAdmin = this._sharedService.isSuperAdmin() || this._sharedService.roles.includes(AuthConstants.ADMIN_ROLE);

    role = '';
    ownerUserId = ALL_OWNERS;
    owners: { userId: string; label: string }[] = [{ userId: ALL_OWNERS, label: 'Mọi trưởng nhóm' }];
    pageNumber = this.START_PAGE_NUMBER;
    rows: IViewStaffGroup[] = [];
    override totalRecords = 0;

    readonly columns: IColumn<IViewStaffGroup>[] = [
        { header: 'Nhóm', key: 'name', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: StaffGroupNameCellComponent },
        { header: 'Vai trò', key: 'role', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: StaffGroupRoleCellComponent },
        { header: 'Số người', field: 'memberCount', headerContainerStyle: 'text-align: right', cellStyle: 'text-align: right' },
        ...(this.isAdmin ? [{ header: 'Trưởng nhóm', field: 'ownerFullName' }] : []),
        { header: 'Ngày tạo', field: 'createdDate', cellViewType: CellViewTypes.DATE },
        {
            header: 'Thao tác',
            key: 'actions',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: StaffGroupActionsCellComponent,
            headerContainerStyle: 'width: 140px; text-align: center',
        },
    ];

    override ngOnInit(): void {
        if (!this.canManage) return;
        this.keyword.valueChanges
            .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef))
            .subscribe((keyword) => this.navigateFilters({ keyword: keyword.trim() }));
        if (this.isAdmin) {
            this.loadOwners();
        }
        this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applyFilters());
    }

    // Lọc nằm trên URL (?role=&ownerUserId=&keyword=): đọc lại mỗi lần URL đổi, đổi lọc thì về trang 1
    applyFilters() {
        const params = this._activatedRoute.snapshot.queryParamMap;
        const role = params.get('role') ?? '';
        this.role = this.tabs.some((tab) => tab.key === role) ? role : '';
        this.ownerUserId = (this.isAdmin && params.get('ownerUserId')) || ALL_OWNERS;
        const keyword = params.get('keyword') ?? '';
        if (this.keyword.value.trim() !== keyword) {
            this.keyword.setValue(keyword, { emitEvent: false });
        }
        this.pageNumber = this.START_PAGE_NUMBER;
        this.loadPage();
    }

    selectTab(role: string) {
        if (role === this.role) return;
        this.navigateFilters({ role });
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với giá trị đang lọc
    onOwnerChange(ownerUserId: string | null) {
        const value = ownerUserId || ALL_OWNERS;
        if (value === this.ownerUserId) return;
        this.navigateFilters({ ownerUserId: value === ALL_OWNERS ? '' : value });
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.loadPage();
    }

    loadPage() {
        this.loading = true;
        this._staffGroupService
            .findPaging({
                role: this.role,
                ownerUserId: this.ownerUserId === ALL_OWNERS ? '' : this.ownerUserId,
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
                this.rows = res.data.items;
                this.totalRecords = res.data.totalItems;
            });
    }

    onAction(event: ITableEvent<IViewStaffGroup>) {
        if (event.type === STAFF_GROUP_ACTIONS.EDIT) {
            this.openForm(event.row!);
        } else if (event.type === STAFF_GROUP_ACTIONS.REMOVE) {
            this.confirmRemove(event.row!);
        }
    }

    // Đóng dialog với true = đã lưu (một phần) hoặc nhóm không còn (801) → tải lại bảng và ô Trưởng nhóm
    openForm(group?: IViewStaffGroup) {
        const data: IStaffGroupDialogData = { group };
        this._dialogService
            .open(StaffGroupFormComponent, { header: group ? 'Sửa nhóm' : 'Tạo nhóm', width: '560px', modal: true, closable: true, data })
            ?.onClose.subscribe((changed) => changed && this.reload());
    }

    private confirmRemove(group: IViewStaffGroup) {
        this._confirmationService.confirm({
            header: `Xoá nhóm “${group.name}”?`,
            message: `Nhóm và danh sách ${group.memberCount} người bị xoá. Người đã được thêm vào dự án từ nhóm này vẫn ở lại dự án.`,
            closable: true,
            closeOnEscape: true,
            icon: 'pi pi-exclamation-triangle',
            rejectButtonProps: { label: 'Huỷ', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Xoá nhóm', severity: 'danger' },
            accept: () => {
                this._staffGroupService
                    .remove(group.id)
                    .pipe(takeUntilDestroyed(this._destroyRef))
                    .subscribe((res) => {
                        if (!this.isResponseSucceed(res)) return this.reload();
                        this.messageSuccess(`Đã xoá nhóm ${group.name}`);
                        // Xoá dòng cuối của trang (không phải trang 1) → lùi 1 trang
                        if (this.rows.length === 1 && this.pageNumber > this.START_PAGE_NUMBER) {
                            this.pageNumber--;
                        }
                        this.reload();
                    });
            },
        });
    }

    private reload() {
        this.loadPage();
        if (this.isAdmin) {
            this.loadOwners();
        }
    }

    private loadOwners() {
        this._staffGroupService
            .owners()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return;
                this.owners = [{ userId: ALL_OWNERS, label: 'Mọi trưởng nhóm' }, ...res.data.map((owner) => ({ userId: owner.userId, label: owner.fullName }))];
            });
    }

    private navigateFilters(change: { role?: string; ownerUserId?: string; keyword?: string }) {
        const queryParams = Object.fromEntries(Object.entries(change).map(([key, value]) => [key, value || null]));
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
    }
}
