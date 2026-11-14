import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Utils } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { SelectButtonModule } from 'primeng/selectbutton';
import { finalize } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { IViewDocumentField } from '@/models/document-field.model';
import { IViewDocumentType } from '@/models/document-type.model';
import { DocumentFieldService } from '@/services/document-field.service';
import { DocumentTypeService } from '@/services/document-type.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { DOCUMENT_TYPE_VIEW_TABLE, DOCUMENT_TYPE_VIEWS } from '@/shared/constants/document-type.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { IColumn, ITableAction, ITableEvent, ITableRowReorder } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { DocumentFieldFormComponent } from './document-field-form/document-field-form.component';
import { DocumentFieldQuickCreateComponent } from './document-field-quick-create/document-field-quick-create.component';
import { DocumentFieldSheetCheckComponent } from './document-field-sheet-check/document-field-sheet-check.component';
import { DocumentTypeFormComponent } from './document-type-form/document-type-form.component';

const DOCUMENT_TYPE_LIST_URL = '/admin/construction/document-type';

const DOCUMENT_FIELD_ACTIONS = {
    EDIT: 'edit',
    DELETE: 'delete',
} as const;

type DocumentFieldRow = IViewDocumentField & { order: number; ocrLabel?: string };
type DocumentFieldSetting = 'isShow' | 'isChargeable';

@Component({
    selector: 'app-document-type-detail',
    imports: [SharedImports, SelectButtonModule, DataTable, PageHeader],
    templateUrl: './document-type-detail.component.html',
})
export class DocumentTypeDetailComponent extends BaseComponent {
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId = Number(this._activatedRoute.snapshot.paramMap.get('id'));

    readonly canUpdateDocumentType = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE);
    readonly canCreateField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE);
    readonly canUpdateField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_UPDATE);
    readonly canDeleteField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_DELETE);

    readonly views = DOCUMENT_TYPE_VIEWS;

    documentType?: IViewDocumentType;
    defaultViewLabel?: string;
    breadcrumbs: MenuItem[] = [];
    keyword = new FormControl('', { nonNullable: true });
    orderView = new FormControl(DOCUMENT_TYPE_VIEW_TABLE, { nonNullable: true });
    fields: DocumentFieldRow[] = [];
    filteredFields: DocumentFieldRow[] = [];
    selectedFields: DocumentFieldRow[] = [];

    private readonly _fieldActions: ITableAction[] = [
        ...(this.canUpdateField ? [{ type: DOCUMENT_FIELD_ACTIONS.EDIT, label: 'Sửa' }] : []),
        ...(this.canDeleteField ? [{ type: DOCUMENT_FIELD_ACTIONS.DELETE, label: 'Xóa' }] : []),
    ];

    readonly columns: IColumn<DocumentFieldRow>[] = [
        { header: '#', field: 'order', headerContainerStyle: 'width: 64px' },
        { header: 'Tên trường', field: 'name' },
        { header: 'Tên cột import', field: 'fieldImport' },
        { header: 'Hiển thị', field: 'isShow', cellViewType: CellViewTypes.SWITCH, headerContainerStyle: 'width: 110px', disabled: () => !this.canUpdateField },
        { header: 'Tính tiền', field: 'isChargeable', cellViewType: CellViewTypes.SWITCH, headerContainerStyle: 'width: 110px', disabled: () => !this.canUpdateField },
        { header: 'OCR', field: 'ocrLabel' },
        ...(this._fieldActions.length
            ? [{ header: '', cellViewType: CellViewTypes.ACTIONS, headerContainerStyle: 'width: 64px', actions: this._fieldActions }]
            : []),
    ];

    get chargeableCount(): number {
        return this.fields.filter((field) => field.isChargeable).length;
    }

    get ocrCount(): number {
        return this.fields.filter((field) => field.isFieldOcr).length;
    }

    // Đang lọc thì vị trí trên bảng không còn là thứ tự thật nên tắt kéo thả
    get canReorder(): boolean {
        return this.canUpdateField && !this.keyword.value.trim();
    }

    override ngOnInit(): void {
        this.keyword.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applyView());
        this.orderView.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applyView());
        this.loadDocumentType();
    }

    loadDocumentType() {
        this._documentTypeService
            .findById(this._documentTypeId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    this.router.navigate([DOCUMENT_TYPE_LIST_URL]);
                    return;
                }
                this.documentType = res.data;
                this.defaultViewLabel = DOCUMENT_TYPE_VIEWS.find((view) => view.value === res.data.defaultView)?.label;
                this.breadcrumbs = [{ label: 'Hồ sơ thi công' }, { label: 'Loại văn bản', routerLink: DOCUMENT_TYPE_LIST_URL }, { label: res.data.name }];
                this.loadFields();
            });
    }

    loadFields() {
        this.loading = true;
        this._documentTypeService
            .getDocumentFields(this._documentTypeId)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.fields = res.data.map((field) => ({ ...field, order: 0, ocrLabel: field.isFieldOcr ? field.fieldNameOcr : undefined }));
                    this.selectedFields = [];
                    this.applyView();
                }
            });
    }

    // Sắp và đánh số theo thứ tự của chế độ đang chọn (hai thứ tự độc lập), rồi lọc từ khoá
    applyView() {
        const isTableView = this.orderView.value === DOCUMENT_TYPE_VIEW_TABLE;
        this.fields.forEach((field) => (field.order = isTableView ? field.orderTableView : field.orderListView));
        this.fields.sort((a, b) => a.order - b.order || a.id - b.id);

        const keyword = this.keyword.value.trim().toLowerCase();
        this.filteredFields = keyword ? this.fields.filter((field) => field.name.toLowerCase().includes(keyword)) : [...this.fields];
    }

    // PrimeNG đã đổi chỗ trong mảng trước khi phát sự kiện; targetOrder = thứ tự đang có ở vị trí thả (dãy thứ tự sắp tăng)
    onRowReorder(event: ITableRowReorder) {
        if (event.dragIndex === event.dropIndex) return;

        const movedField = this.filteredFields[event.dropIndex];
        const body = { targetOrder: this.filteredFields.map((field) => field.order).sort((a, b) => a - b)[event.dropIndex] };
        const request$ =
            this.orderView.value === DOCUMENT_TYPE_VIEW_TABLE
                ? this._documentFieldService.changeOrderTableView(movedField.id, body)
                : this._documentFieldService.changeOrderListView(movedField.id, body);

        this.loading = true;
        request$
            .pipe(
                finalize(() => this.loadFields()),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => this.isResponseSucceed(res));
    }

    onAction(event: ITableEvent<DocumentFieldRow>) {
        if (event.type === TableEventTypes.CELL_CLICK) {
            this.toggleSetting(event.row!, event.field as DocumentFieldSetting);
        } else if (event.type === DOCUMENT_FIELD_ACTIONS.EDIT) {
            this.openFieldForm(event.row);
        } else if (event.type === DOCUMENT_FIELD_ACTIONS.DELETE) {
            this.confirmDeleteField(event.row!);
        }
    }

    openDocumentTypeForm() {
        this._dialogService
            .open(DocumentTypeFormComponent, { header: 'Sửa loại văn bản', width: '32rem', modal: true, closable: true, data: { documentType: this.documentType } })
            ?.onClose.subscribe((changed) => changed && this.loadDocumentType());
    }

    openFieldForm(documentField?: IViewDocumentField) {
        this._dialogService
            .open(DocumentFieldFormComponent, {
                header: documentField ? 'Sửa trường' : 'Thêm trường',
                width: '40rem',
                modal: true,
                closable: true,
                data: { documentTypeId: this._documentTypeId, documentField },
            })
            ?.onClose.subscribe((changed) => changed && this.loadFields());
    }

    openQuickCreate() {
        this._dialogService
            .open(DocumentFieldQuickCreateComponent, {
                header: 'Thêm nhanh trường',
                width: '40rem',
                modal: true,
                closable: true,
                data: { documentTypeId: this._documentTypeId },
            })
            ?.onClose.subscribe((changed) => changed && this.loadFields());
    }

    openSheetCheck() {
        this._dialogService.open(DocumentFieldSheetCheckComponent, {
            header: 'Kiểm tra với Google Sheet',
            width: '48rem',
            modal: true,
            closable: true,
            data: { documentTypeId: this._documentTypeId },
        });
    }

    downloadTemplate() {
        const url = this._documentTypeService.excelTemplateUrl(this._documentTypeId);
        this._downloadService
            .get(url)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    this.router.navigate([DOCUMENT_TYPE_LIST_URL]);
                    return;
                }
                Utils.makeDownload(response, url);
            });
    }

    confirmQuickDelete() {
        const ids = this.selectedFields.map((field) => field.id);
        this.confirmDelete({ header: 'Xóa trường', message: `Xóa ${ids.length} trường đã chọn?` }, () => {
            this._documentFieldService
                .quickDelete({ documentTypeId: this._documentTypeId, ids })
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, `Đã xóa ${ids.length} trường`)) {
                        this.loadFields();
                    }
                });
        });
    }

    // Lưu ngay khi bấm; API setting ghi đè cả 4 cột nên gửi đủ giá trị hiện tại của dòng, lỗi thì trả công tắc về cũ
    private toggleSetting(field: DocumentFieldRow, setting: DocumentFieldSetting) {
        field[setting] = !field[setting];
        const revert = () => (field[setting] = !field[setting]);
        this._documentFieldService
            .updateSetting(field.id, { isShow: field.isShow, isChargeable: field.isChargeable, isFieldOcr: field.isFieldOcr, fieldNameOcr: field.fieldNameOcr })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({ next: (res) => !this.isResponseSucceed(res) && revert(), error: revert });
    }

    private confirmDeleteField(field: IViewDocumentField) {
        this.confirmDelete({ header: 'Xóa trường', message: `Xóa trường ${field.name}?` }, () => {
            this._documentFieldService
                .delete(field.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, 'Xóa trường thành công')) {
                        this.loadFields();
                    }
                });
        });
    }
}
