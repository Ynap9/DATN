import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputNumberComponent, ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { FieldsetModule } from 'primeng/fieldset';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';
import { IViewDocumentField } from '@/models/document-field.model';
import { DocumentFieldService } from '@/services/document-field.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-document-field-form',
    imports: [SharedImports, FieldsetModule, ToggleSwitchModule, ShInputTextComponent, ShInputNumberComponent],
    templateUrl: './document-field-form.component.html',
})
export class DocumentFieldFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId: number = this._config.data.documentTypeId;
    readonly documentField: IViewDocumentField | undefined = this._config.data.documentField;

    submitting = false;

    override form = new FormGroup({
        name: new FormControl(this.documentField?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
        fieldImport: new FormControl(this.documentField?.fieldImport ?? '', { nonNullable: true }),
        isShow: new FormControl(this.documentField?.isShow ?? false, { nonNullable: true }),
        isChargeable: new FormControl(this.documentField?.isChargeable ?? false, { nonNullable: true }),
        isFieldOcr: new FormControl(this.documentField?.isFieldOcr ?? false, { nonNullable: true }),
        fieldNameOcr: new FormControl(this.documentField?.fieldNameOcr ?? '', {
            nonNullable: true,
            validators: this.documentField?.isFieldOcr ? [Validators.required] : [],
        }),
        isShowTableView: new FormControl(this.documentField?.isShowTableView ?? true, { nonNullable: true }),
        isShowListView: new FormControl(this.documentField?.isShowListView ?? true, { nonNullable: true }),
        width: new FormControl<number | null>(this.documentField?.width ?? null),
    });

    override ngOnInit(): void {
        const { isFieldOcr, fieldNameOcr } = this.form.controls;
        isFieldOcr.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((isOcr) => {
            fieldNameOcr.setValidators(isOcr ? [Validators.required] : []);
            fieldNameOcr.updateValueAndValidity();
        });
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const { name, fieldImport, isShow, isChargeable, isFieldOcr, fieldNameOcr, isShowTableView, isShowListView, width } = this.form.getRawValue();
        // Mã trường không dùng trong nghiệp vụ nên lấy bằng tên; cột import trống thì import sẽ bỏ qua trường nên điền bằng tên
        const common = { name, field: name, fieldImport: fieldImport || name, isShow, isChargeable, isFieldOcr, fieldNameOcr };
        const request$ = this.documentField
            ? this._documentFieldService.update(this.documentField.id, {
                  ...common,
                  fieldType: this.documentField.fieldType,
                  order: 0,
                  isShowTableView,
                  isShowListView,
                  width: width ?? undefined,
              })
            : this._documentFieldService.create({ ...common, documentTypeId: this._documentTypeId });

        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Lưu trường thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
