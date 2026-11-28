import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputNumberComponent, ShInputTextComponent, ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { FieldsetModule } from 'primeng/fieldset';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';
import { IViewDocumentEnum } from '@/models/document-enum.model';
import { IViewDocumentField } from '@/models/document-field.model';
import { DocumentEnumService } from '@/services/document-enum.service';
import { DocumentFieldService } from '@/services/document-field.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DOCUMENT_FIELD_TYPE, DOCUMENT_FIELD_TYPES } from '@/shared/constants/document-field.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { SharedImports } from '@/shared/import.shared';
import { DocumentEnumFormComponent } from '../document-enum-form/document-enum-form.component';

@Component({
    selector: 'app-document-field-form',
    imports: [SharedImports, FieldsetModule, SelectButtonModule, ToggleSwitchModule, ShInputTextComponent, ShInputNumberComponent, ShSelectComponent],
    templateUrl: './document-field-form.component.html',
})
export class DocumentFieldFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _documentEnumService = inject(DocumentEnumService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId: number = this._config.data.documentTypeId;
    readonly documentField: IViewDocumentField | undefined = this._config.data.documentField;

    readonly fieldTypes = DOCUMENT_FIELD_TYPES;
    readonly fieldTypeEnum = DOCUMENT_FIELD_TYPE.ENUM;
    readonly canCreateEnum = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE);

    submitting = false;
    enums: IViewDocumentEnum[] = [];

    override form = new FormGroup({
        name: new FormControl(this.documentField?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
        fieldType: new FormControl(this.documentField?.fieldType ?? DOCUMENT_FIELD_TYPE.STRING, { nonNullable: true }),
        enumId: new FormControl<number | null>(this.documentField?.enumId ?? null, {
            validators: this.documentField?.fieldType === DOCUMENT_FIELD_TYPE.ENUM ? [Validators.required] : [],
        }),
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
        const { isFieldOcr, fieldNameOcr, fieldType, enumId } = this.form.controls;
        fieldType.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((type) => {
            enumId.setValidators(type === DOCUMENT_FIELD_TYPE.ENUM ? [Validators.required] : []);
            enumId.updateValueAndValidity();
        });
        this.loadEnums();
        isFieldOcr.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((isOcr) => {
            fieldNameOcr.setValidators(isOcr ? [Validators.required] : []);
            fieldNameOcr.updateValueAndValidity();
        });
    }

    loadEnums(selectedEnumId?: number) {
        this._documentEnumService
            .getList(this._documentTypeId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.enums = res.data;
                    if (selectedEnumId) {
                        this.form.controls.enumId.setValue(selectedEnumId);
                    }
                }
            });
    }

    // Tạo danh mục ngay trong form trường: tạo xong tự chọn danh mục vừa tạo
    openEnumForm() {
        this._dialogService
            .open(DocumentEnumFormComponent, { header: 'Thêm danh mục', width: '36rem', modal: true, closable: true, data: { documentTypeId: this._documentTypeId } })
            ?.onClose.subscribe((enumId: number | false) => enumId && this.loadEnums(enumId));
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const { name, fieldType, enumId, fieldImport, isShow, isChargeable, isFieldOcr, fieldNameOcr, isShowTableView, isShowListView, width } = this.form.getRawValue();
        // Mã trường không dùng trong nghiệp vụ nên lấy bằng tên; cột import trống thì import sẽ bỏ qua trường nên điền bằng tên
        const common = { name, field: name, fieldType, enumId: fieldType === DOCUMENT_FIELD_TYPE.ENUM ? (enumId ?? undefined) : undefined, fieldImport: fieldImport || name, isShow, isChargeable, isFieldOcr, fieldNameOcr };
        const request$ = this.documentField
            ? this._documentFieldService.update(this.documentField.id, {
                  ...common,
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
