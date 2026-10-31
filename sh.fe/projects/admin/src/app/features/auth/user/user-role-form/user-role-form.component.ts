import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShMultiSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { IRoleDropdown } from '@/models/role.model';
import { IViewUser } from '@/models/user.model';
import { RoleService } from '@/services/role.service';
import { UserService } from '@/services/user.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-user-role-form',
    imports: [SharedImports, ShMultiSelectComponent],
    templateUrl: './user-role-form.component.html',
})
export class UserRoleFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _userService = inject(UserService);
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _user: IViewUser = this._config.data.user;

    roles: IRoleDropdown[] = [];
    submitting = false;

    override form = new FormGroup({
        roleId: new FormControl<number[]>(this._user.roles.map((role) => role.id), { nonNullable: true, validators: [Validators.required] }),
    });

    override ngOnInit(): void {
        this._roleService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.roles = res.data;
                }
            });
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        this.submitting = true;
        this._userService
            .updateRole({ userId: this._user.id, roleId: this.form.getRawValue().roleId })
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Cập nhật vai trò thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
