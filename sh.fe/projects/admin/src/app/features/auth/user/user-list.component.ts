import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { IRoleDropdown } from '@/models/role.model';
import { IViewUser } from '@/models/user.model';
import { RoleService } from '@/services/role.service';
import { UserService } from '@/services/user.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { UserFormComponent } from './user-form/user-form.component';
import { UserRoleFormComponent } from './user-role-form/user-role-form.component';

const USER_ACTIONS = {
    ASSIGN_ROLE: 'assignRole',
    DELETE: 'delete',
} as const;

type UserRow = IViewUser & { roleNames: string; statusLabel: string };

@Component({
    selector: 'app-user-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent],
    templateUrl: './user-list.component.html',
})
export class UserListComponent extends BaseComponent {
    private readonly _userService = inject(UserService);
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Quản trị' }, { label: 'Người dùng' }];

    keyword = new FormControl('', { nonNullable: true });
    roleId = new FormControl<number | null>(null);
    roles: IRoleDropdown[] = [];
    users: UserRow[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = 25;
    override totalRecords = 0;

    readonly columns: IColumn<UserRow>[] = [
        { header: 'Họ tên', field: 'fullName' },
        { header: 'Tên đăng nhập', field: 'userName' },
        { header: 'Email', field: 'email' },
        { header: 'Vai trò', field: 'roleNames' },
        {
            header: 'Trạng thái',
            field: 'statusLabel',
            cellViewType: CellViewTypes.STATUS,
            statusSeverity: (row) => (row.isLocked ? 'danger' : 'success'),
        },
        { header: 'Người tạo', field: 'createdBy' },
        { header: 'Ngày tạo', field: 'createdDate', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm:ss' },
        {
            header: '',
            cellViewType: CellViewTypes.ACTIONS,
            headerContainerStyle: 'width: 64px',
            actions: [
                { type: USER_ACTIONS.ASSIGN_ROLE, label: 'Gán vai trò', icon: 'pi pi-shield' },
                { type: USER_ACTIONS.DELETE, label: 'Xóa', icon: 'pi pi-trash' },
            ],
        },
    ];

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.search());

        this._roleService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.roles = res.data;
                }
            });
        this.load();
    }

    load() {
        const roleId = this.roleId.value;

        this.loading = true;
        this._userService
            .findPaging({ pageNumber: this.pageNumber, pageSize: this.pageSize, keyword: this.keyword.value, ...(roleId ? { roleId } : {}) })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.users = res.data.items.map((user) => ({
                        ...user,
                        roleNames: user.roles.map((role) => role.name).join(', '),
                        statusLabel: user.isLocked ? 'Đã khóa' : 'Đang hoạt động',
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

    onAction(event: ITableEvent<IViewUser>) {
        if (event.type === USER_ACTIONS.ASSIGN_ROLE) {
            this.openRoleForm(event.row!);
        } else if (event.type === USER_ACTIONS.DELETE) {
            this.confirmDeleteUser(event.row!);
        }
    }

    openCreateForm() {
        this._dialogService
            .open(UserFormComponent, { header: 'Thêm người dùng', width: '40rem', modal: true, closable: true })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    search() {
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }

    private openRoleForm(user: IViewUser) {
        this._dialogService
            .open(UserRoleFormComponent, { header: `Gán vai trò: ${user.userName}`, width: '32rem', modal: true, closable: true, data: { user } })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    private confirmDeleteUser(user: IViewUser) {
        this.confirmDelete({ header: 'Xóa người dùng', message: `Xóa người dùng ${user.userName}?` }, () => {
            this._userService
                .delete(user.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, 'Xóa người dùng thành công')) {
                        this.load();
                    }
                });
        });
    }
}
