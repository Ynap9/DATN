import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { SelectButtonModule } from 'primeng/selectbutton';
import { finalize } from 'rxjs';
import { IViewDocumentEnum } from '@/models/document-enum.model';
import { DocumentEnumService } from '@/services/document-enum.service';
import { BaseComponent } from '@/shared/base/base.component';
import {
    DOCUMENT_ENUM_INPUT_MODE,
    DOCUMENT_ENUM_INPUT_MODES,
    DOCUMENT_ENUM_NAME_MAX_LENGTH,
    DOCUMENT_ENUM_VALUE_MAX_LENGTH,
    DOCUMENT_ENUM_VALUES_MAX_COUNT,
} from '@/shared/constants/document-field.constants';
import { SharedImports } from '@/shared/import.shared';

interface IEnumValueSummary {
    values: string[];
    duplicateCount: number;
    error: string | null;
}

// Dialog thêm / sửa danh mục; đóng với Id danh mục đã lưu để chỗ gọi tải lại (form trường dùng Id đó để chọn luôn danh mục vừa tạo)
@Component({
    selector: 'app-document-enum-form',
    imports: [SharedImports, SelectButtonModule, ShInputTextComponent],
    templateUrl: './document-enum-form.component.html',
})
export class DocumentEnumFormComponent extends BaseComponent implements OnInit {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentEnumService = inject(DocumentEnumService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId: number = this._config.data.documentTypeId;
    readonly documentEnum: IViewDocumentEnum | undefined = this._config.data.documentEnum;

    readonly inputModes = DOCUMENT_ENUM_INPUT_MODES;
    readonly modeSingle = DOCUMENT_ENUM_INPUT_MODE.SINGLE;
    readonly nameMaxLength = DOCUMENT_ENUM_NAME_MAX_LENGTH;
    readonly valuesMaxCount = DOCUMENT_ENUM_VALUES_MAX_COUNT;

    submitting = false;
    // Chế độ "Từng giá trị" giữ danh sách ở đây; chế độ "Dán hàng loạt" giữ chữ trong bulkText, đổi chế độ thì chuyển qua lại
    values: string[] = [...(this.documentEnum?.values ?? [])];
    private _dragIndex: number | null = null;

    inputMode = new FormControl<string>(DOCUMENT_ENUM_INPUT_MODE.SINGLE, { nonNullable: true });
    newValue = new FormControl('', { nonNullable: true });
    bulkText = new FormControl('', { nonNullable: true });

    override form = new FormGroup({
        name: new FormControl(this.documentEnum?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
    });

    // Giá trị đang nhập đã có trong danh sách (không phân biệt hoa thường) thì Enter không thêm
    get isDuplicateInput(): boolean {
        const value = this.newValue.value.trim().toLowerCase();
        return !!value && this.values.some((existing) => existing.toLowerCase() === value);
    }

    override ngOnInit(): void {
        this.inputMode.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((mode) => {
            if (mode === DOCUMENT_ENUM_INPUT_MODE.BULK) {
                this.bulkText.setValue(this.values.join('\n'));
            } else {
                this.values = this.summarize(this.bulkText.value.split('\n')).values;
            }
        });
    }

    // Giá trị của chế độ đang chọn sau khi chuẩn hoá
    valueSummary(): IEnumValueSummary {
        return this.summarize(this.inputMode.value === DOCUMENT_ENUM_INPUT_MODE.SINGLE ? this.values : this.bulkText.value.split('\n'));
    }

    // Chuẩn hoá như BE: trim từng dòng, bỏ dòng trống, bỏ trùng không phân biệt hoa thường (giữ cách viết lần đầu); báo lỗi trước khi gửi
    private summarize(rawLines: string[]): IEnumValueSummary {
        const lines = rawLines.map((line) => line.trim()).filter((line) => line.length > 0);
        const seen = new Set<string>();
        const values = lines.filter((line) => {
            const key = line.toLowerCase();
            return !seen.has(key) && !!seen.add(key);
        });

        let error: string | null = null;
        if (values.length === 0) {
            error = 'Danh mục cần ít nhất 1 giá trị';
        } else if (values.length > DOCUMENT_ENUM_VALUES_MAX_COUNT) {
            error = `Tối đa ${DOCUMENT_ENUM_VALUES_MAX_COUNT} giá trị`;
        } else if (values.some((value) => value.length > DOCUMENT_ENUM_VALUE_MAX_LENGTH)) {
            error = `Mỗi giá trị tối đa ${DOCUMENT_ENUM_VALUE_MAX_LENGTH} ký tự`;
        }
        return { values, duplicateCount: lines.length - values.length, error };
    }

    // Enter ở ô nhập thêm giá trị, không gửi form
    addValue(event: Event) {
        event.preventDefault();
        const value = this.newValue.value.trim();
        if (!value || this.isDuplicateInput) return;

        this.values.push(value);
        this.newValue.reset();
    }

    removeValue(index: number) {
        this.values.splice(index, 1);
    }

    startDrag(index: number) {
        this._dragIndex = index;
    }

    // Thả vào vị trí mới: chèn giá trị đang kéo vào đó
    dropValue(index: number) {
        if (this._dragIndex === null || this._dragIndex === index) return;

        const [moved] = this.values.splice(this._dragIndex, 1);
        this.values.splice(index, 0, moved);
        this._dragIndex = null;
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        const summary = this.valueSummary();
        if (this.isFormInvalid() || summary.error) return;

        const body = { name: this.form.controls.name.value.trim(), values: summary.values };
        const request$ = this.documentEnum
            ? this._documentEnumService.update(this._documentTypeId, this.documentEnum.id, body)
            : this._documentEnumService.create(this._documentTypeId, body);

        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Lưu danh mục thành công')) {
                    this._ref.close('data' in res ? res.data : this.documentEnum!.id);
                }
            });
    }
}
