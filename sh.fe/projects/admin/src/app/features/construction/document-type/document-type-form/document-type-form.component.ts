import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { SelectButtonModule } from 'primeng/selectbutton';
import { finalize } from 'rxjs';
import { IViewDocumentType } from '@/models/document-type.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DOCUMENT_TYPE_VIEWS } from '@/shared/constants/document-type.constants';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-document-type-form',
    imports: [SharedImports, SelectButtonModule, ShInputTextComponent],
    templateUrl: './document-type-form.component.html',
})
export class DocumentTypeFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentType: IViewDocumentType | undefined = this._config.data?.documentType;
    private readonly _defaultView = this._documentType?.defaultView ?? DOCUMENT_TYPE_VIEWS[0].value;

    readonly views = DOCUMENT_TYPE_VIEWS;
    submitting = false;

    override form = new FormGroup({
        name: new FormControl(this._documentType?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
        // Dữ liệu cũ có thể mang kiểu xem ngoài 2 giá trị: để trống, bắt chọn lại
        defaultView: new FormControl<number | null>(this.views.some((view) => view.value === this._defaultView) ? this._defaultView : null, {
            validators: [Validators.required],
        }),
    });

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const body = { name: this.form.controls.name.value, defaultView: this.form.controls.defaultView.value! };
        const request$ = this._documentType ? this._documentTypeService.update(this._documentType.id, body) : this._documentTypeService.create(body);

        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Lưu metadata thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
