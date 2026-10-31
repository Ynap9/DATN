import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MenuItem } from 'primeng/api';
import { finalize } from 'rxjs';
import { IViewRole } from '@/models/role.model';
import { RoleService } from '@/services/role.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { SharedImports } from '@/shared/import.shared';
import { RoleFormComponent } from './role-form/role-form.component';
import { RolePermissionComponent } from './role-permission/role-permission.component';

const ALL_RECORDS_PAGE_SIZE = -1;

@Component({
    selector: 'app-role-list',
    imports: [SharedImports, PageHeader, RolePermissionComponent],
    templateUrl: './role-list.component.html',
    styleUrl: './role-list.component.scss',
})
export class RoleListComponent extends BaseComponent {
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _permissionPanel = viewChild(RolePermissionComponent);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Quản trị' }, { label: 'Vai trò và phân quyền' }];

    keyword = '';
    roles: IViewRole[] = [];
    selectedRole: IViewRole | null = null;

    get filteredRoles(): IViewRole[] {
        const keyword = this.keyword.trim().toLowerCase();
        return this.roles.filter((role) => role.name.toLowerCase().includes(keyword));
    }

    override ngOnInit(): void {
        this.load();
    }

    load() {
        this.loading = true;
        this._roleService
            .findPaging({ pageNumber: this.START_PAGE_NUMBER, pageSize: ALL_RECORDS_PAGE_SIZE, keyword: '' })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.roles = res.data.items;
                    this.selectedRole = this.roles.find((role) => role.id === this.selectedRole?.id) ?? this.roles[0] ?? null;
                }
            });
    }

    selectRole(role: IViewRole) {
        if (role.id === this.selectedRole?.id) return;

        if (this._permissionPanel()?.isDirty) {
            this.confirmAction({ header: 'Chưa lưu thay đổi', message: 'Quyền vừa chỉnh chưa được lưu. Chuyển vai trò và bỏ thay đổi?' }, () => (this.selectedRole = role));
        } else {
            this.selectedRole = role;
        }
    }

    openForm(role?: IViewRole) {
        this._dialogService
            .open(RoleFormComponent, { header: role ? 'Sửa vai trò' : 'Thêm vai trò', width: '32rem', modal: true, closable: true, data: { role } })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    confirmDeleteRole(role: IViewRole) {
        this.confirmDelete({ header: 'Xóa vai trò', message: `Xóa vai trò ${role.name}?` }, () => {
            this._roleService
                .delete(role.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, 'Xóa vai trò thành công')) {
                        this.load();
                    }
                });
        });
    }
}
