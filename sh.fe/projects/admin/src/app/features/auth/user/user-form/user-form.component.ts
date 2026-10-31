import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputTextComponent, ShMultiSelectComponent } from '@sh/ui';
import { CheckboxModule } from 'primeng/checkbox';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { IRoleDropdown } from '@/models/role.model';
import { RoleService } from '@/services/role.service';
import { UserService } from '@/services/user.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-user-form',
    imports: [SharedImports, CheckboxModule, ShInputTextComponent, ShMultiSelectComponent],
    templateUrl: './user-form.component.html',
})
export class UserFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _userService = inject(UserService);
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);

    roles: IRoleDropdown[] = [];
    submitting = false;

    override form = new FormGroup({
        userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
        fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
        email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
        phoneNumber: new FormControl('', { nonNullable: true }),
        roleId: new FormControl<number[]>([], { nonNullable: true, validators: [Validators.required] }),
        isRandomPassword: new FormControl(true, { nonNullable: true }),
        password: new FormControl({ value: '', disabled: true }, { nonNullable: true, validators: [Validators.required] }),
        passwordConfirm: new FormControl({ value: '', disabled: true }, { nonNullable: true, validators: [Validators.required] }),
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

        this.form.controls.isRandomPassword.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((isRandom) => {
            const { password, passwordConfirm } = this.form.controls;
            if (isRandom) {
                password.disable();
                passwordConfirm.disable();
            } else {
                password.enable();
                passwordConfirm.enable();
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
            .create(this.form.getRawValue())
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Thêm người dùng thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
