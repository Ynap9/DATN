import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ShInputNumberComponent, ShInputTextComponent, ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize, merge } from 'rxjs';
import { ISheetHeader } from '@/models/document-field.model';
import { DocumentFieldService } from '@/services/document-field.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';

type FieldImportMatchRow = { fieldImport?: string; matchedHeaders: string; isMatched: boolean; statusLabel: string };

@Component({
    selector: 'app-document-field-sheet-check',
    imports: [SharedImports, DataTable, ShInputTextComponent, ShSelectComponent, ShInputNumberComponent],
    templateUrl: './document-field-sheet-check.component.html',
})
export class DocumentFieldSheetCheckComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId: number = this._config.data.documentTypeId;

    sheets: ISheetHeader[] = [];
    matches: FieldImportMatchRow[] = [];
    errorMessage = '';
    loadingSheets = false;
    checking = false;

    override form = new FormGroup({
        url: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
        sheetName: new FormControl<string | null>(null, { validators: [Validators.required] }),
        headerStartIndex: new FormControl<number | null>(1, { validators: [Validators.required, Validators.min(1)] }),
    });

    readonly columns: IColumn<FieldImportMatchRow>[] = [
        { header: 'Tên cột import', field: 'fieldImport' },
        { header: 'Cột khớp trên sheet', field: 'matchedHeaders' },
        {
            header: 'Kết quả',
            field: 'statusLabel',
            cellViewType: CellViewTypes.STATUS,
            headerContainerStyle: 'width: 140px',
            statusSeverity: (row) => (row.isMatched ? 'success' : 'danger'),
        },
    ];

    get matchedCount(): number {
        return this.matches.filter((match) => match.isMatched).length;
    }

    override ngOnInit(): void {
        // Bước sau chỉ dùng được khi bước trước xong: đổi link thì bỏ danh sách sheet, đổi sheet / dòng tiêu đề thì bỏ kết quả
        const { url, sheetName, headerStartIndex } = this.form.controls;
        url.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => {
            this.sheets = [];
            sheetName.reset();
        });
        merge(url.valueChanges, sheetName.valueChanges, headerStartIndex.valueChanges)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(() => (this.matches = []));
    }

    close() {
        this._ref.close();
    }

    loadSheets() {
        const { url } = this.form.controls;
        if (url.invalid) {
            url.markAsTouched();
            return;
        }

        this.errorMessage = '';
        this.loadingSheets = true;
        this._documentFieldService
            .getSheetHeaders(url.value)
            .pipe(
                finalize(() => (this.loadingSheets = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.errorMessage = res.message;
                    return;
                }
                this.sheets = res.data;
                this.form.controls.sheetName.setValue(res.data[0]?.sheetName ?? null);
            });
    }

    onSubmit() {
        if (!this.sheets.length) {
            this.loadSheets();
            return;
        }
        if (this.isFormInvalid()) return;

        const { url, sheetName, headerStartIndex } = this.form.getRawValue();
        this.errorMessage = '';
        this.checking = true;
        this._documentFieldService
            .mapFieldImport({ documentTypeId: this._documentTypeId, url, sheetName: sheetName!, headerStartIndex: headerStartIndex! })
            .pipe(
                finalize(() => (this.checking = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.errorMessage = res.message;
                    return;
                }
                this.matches = res.data.map((match) => ({
                    fieldImport: match.fieldImport,
                    matchedHeaders: match.headers.join(', '),
                    isMatched: match.headers.length > 0,
                    statusLabel: match.headers.length ? 'Khớp' : 'Không khớp',
                }));
            });
    }
}
