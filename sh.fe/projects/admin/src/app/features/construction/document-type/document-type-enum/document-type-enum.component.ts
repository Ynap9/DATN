import { Component, DestroyRef, inject, input, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { IViewDocumentEnum } from '@/models/document-enum.model';
import { DocumentEnumService } from '@/services/document-enum.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn, ITableAction, ITableEvent } from '@/shared/models/data-table.models';
import { DocumentEnumFormComponent } from '../document-enum-form/document-enum-form.component';
import { EnumValuesCellComponent } from './enum-values-cell.component';

const DOCUMENT_ENUM_ACTIONS = {
    EDIT: 'edit',
    DELETE: 'delete',
} as const;

type DocumentEnumRow = IViewDocumentEnum & { valueCount: number; usedByText: string };

// Tab "Danh mục" của chi tiết Metadata: danh sách danh mục, thêm / sửa / xoá (cần quyền sửa metadata)
@Component({
    selector: 'app-document-type-enum',
    imports: [SharedImports, DataTable],
    templateUrl: './document-type-enum.component.html',
})
export class DocumentTypeEnumComponent extends BaseComponent implements OnInit {
    private readonly _documentEnumService = inject(DocumentEnumService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly documentTypeId = input.required<number>();

    readonly canUpdate = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE);

    rows: DocumentEnumRow[] = [];

    private readonly _actions: ITableAction[] = [
        { type: DOCUMENT_ENUM_ACTIONS.EDIT, label: 'Sửa', icon: 'pi pi-pencil' },
        { type: DOCUMENT_ENUM_ACTIONS.DELETE, label: 'Xóa', icon: 'pi pi-trash', severity: 'danger' },
    ];

    readonly columns: IColumn<DocumentEnumRow>[] = [
        { header: 'Tên', field: 'name', headerContainerStyle: 'width: 240px' },
        { header: 'Số giá trị', field: 'valueCount', headerContainerStyle: 'width: 110px; text-align: center', cellStyle: 'text-align: center' },
        { header: 'Giá trị', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: EnumValuesCellComponent },
        { header: 'Dùng ở trường', field: 'usedByText', headerContainerStyle: 'width: 220px' },
        ...(this.canUpdate
            ? [{ header: 'Thao tác', cellViewType: CellViewTypes.ACTIONS, headerContainerStyle: 'width: 100px; text-align: center', cellStyle: 'text-align: center', actions: this._actions }]
            : []),
    ];

    override ngOnInit(): void {
        this.loadEnums();
    }

    loadEnums() {
        this.loading = true;
        this._documentEnumService
            .getList(this.documentTypeId())
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.rows = res.data.map((documentEnum) => ({
                        ...documentEnum,
                        valueCount: documentEnum.values.length,
                        usedByText: documentEnum.usedByFields.map((field) => field.name).join(', ') || 'Chưa dùng',
                    }));
                }
            });
    }

    openForm(documentEnum?: IViewDocumentEnum) {
        this._dialogService
            .open(DocumentEnumFormComponent, {
                header: documentEnum ? 'Sửa danh mục' : 'Thêm danh mục',
                width: '36rem',
                modal: true,
                closable: true,
                data: { documentTypeId: this.documentTypeId(), documentEnum },
            })
            ?.onClose.subscribe((changed) => changed && this.loadEnums());
    }

    onAction(event: ITableEvent<DocumentEnumRow>) {
        if (event.type === DOCUMENT_ENUM_ACTIONS.EDIT) {
            this.openForm(event.row);
        } else if (event.type === DOCUMENT_ENUM_ACTIONS.DELETE) {
            this.confirmDeleteEnum(event.row!);
        }
    }

    // Còn trường dùng danh mục thì BE trả 350 kèm tên trường, toast hiện nguyên câu đó
    private confirmDeleteEnum(documentEnum: IViewDocumentEnum) {
        this.confirmDelete({ header: 'Xóa danh mục', message: `Xóa danh mục ${documentEnum.name}?` }, () => {
            this._documentEnumService
                .delete(this.documentTypeId(), documentEnum.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, 'Xóa danh mục thành công')) {
                        this.loadEnums();
                    }
                });
        });
    }
}
