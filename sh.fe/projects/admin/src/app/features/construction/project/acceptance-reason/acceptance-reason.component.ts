import { Component, inject } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { BaseComponent } from '@/shared/base/base.component';
import { ACCEPTANCE_REASON_MAX, ACCEPTANCE_REASON_MIN } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-acceptance-reason',
    imports: [SharedImports, ShInputTextComponent],
    templateUrl: './acceptance-reason.component.html',
})
export class AcceptanceReasonComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);

    readonly message: string = this._config.data?.message ?? '';
    readonly confirmLabel: string = this._config.data?.confirmLabel ?? 'Xác nhận';
    readonly isRequired: boolean = this._config.data?.isRequired ?? false;
    readonly maxLength = String(ACCEPTANCE_REASON_MAX);

    override form = new FormGroup({
        reason: new FormControl('', { nonNullable: true, validators: [(control) => this.validateReason(control)] }),
    });

    close() {
        this._ref.close();
    }

    onSubmit() {
        if (this.isFormInvalid()) return;
        this._ref.close(this.form.controls.reason.value.trim());
    }

    // Ô nhập đã chặn quá ACCEPTANCE_REASON_MAX ký tự; chỉ còn kiểm tối thiểu sau trim như BE.
    // ShBase.setRequired gọi thử validator với {} (value undefined) nên phải chặn rỗng
    private validateReason(control: AbstractControl): ValidationErrors | null {
        return this.isRequired && String(control.value ?? '').trim().length < ACCEPTANCE_REASON_MIN
            ? { errorMessage: `Lý do phải từ ${ACCEPTANCE_REASON_MIN} đến ${ACCEPTANCE_REASON_MAX} ký tự` }
            : null;
    }
}
