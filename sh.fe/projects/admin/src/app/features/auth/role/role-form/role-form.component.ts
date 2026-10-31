import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { IViewRole } from '@/models/role.model';
import { RoleService } from '@/services/role.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-role-form',
    imports: [SharedImports, ShInputTextComponent],
    templateUrl: './role-form.component.html',
})
export class RoleFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _role: IViewRole | undefined = this._config.data.role;

    submitting = false;

    override form = new FormGroup({
        name: new FormControl(this._role?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
        description: new FormControl(this._role?.description ?? '', { nonNullable: true }),
    });

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const body = this.form.getRawValue();
        const request$ = this._role ? this._roleService.update(this._role.id, body) : this._roleService.create({ id: 0, ...body });

        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Lưu vai trò thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
