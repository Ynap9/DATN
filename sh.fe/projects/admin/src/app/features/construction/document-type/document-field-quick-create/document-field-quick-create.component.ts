import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize, map } from 'rxjs';
import { DocumentFieldService } from '@/services/document-field.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

const FIELD_NAME_SEPARATOR = ';';

@Component({
    selector: 'app-document-field-quick-create',
    imports: [SharedImports, ShInputTextComponent],
    templateUrl: './document-field-quick-create.component.html',
})
export class DocumentFieldQuickCreateComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId: number = this._config.data.documentTypeId;

    submitting = false;

    override form = new FormGroup({
        fieldNames: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    });

    // Tách đúng luật BE SplitFieldNames: trim, bỏ rỗng, bỏ trùng không phân biệt hoa thường, giữ lần đầu
    readonly previewNames = toSignal(
        this.form.controls.fieldNames.valueChanges.pipe(
            map((value) => {
                const names = value.split(FIELD_NAME_SEPARATOR).map((name) => name.trim()).filter((name) => name);
                return names.filter((name, index) => names.findIndex((other) => other.toLowerCase() === name.toLowerCase()) === index);
            }),
        ),
        { initialValue: [] as string[] },
    );

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid() || !this.previewNames().length) return;

        this.submitting = true;
        this._documentFieldService
            .quickCreate({ documentTypeId: this._documentTypeId, fieldNames: this.form.controls.fieldNames.value })
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, `Đã thêm ${this.previewNames().length} trường`)) {
                    this._ref.close(true);
                }
            });
    }
}
