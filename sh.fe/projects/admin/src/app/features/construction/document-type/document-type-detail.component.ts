import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Utils } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Tabs, TabsModule } from 'primeng/tabs';
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
import { DOCUMENT_TYPE_VIEW_TABLE, DOCUMENT_TYPE_VIEWS, METADATA_TAB, METADATA_URL } from '@/shared/constants/document-type.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { IColumn, ITableAction, ITableEvent, ITableRowReorder } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { DocumentFieldNameCellComponent } from './document-field-name-cell/document-field-name-cell.component';
import { DocumentFieldFormComponent } from './document-field-form/document-field-form.component';
import { DocumentFieldQuickCreateComponent } from './document-field-quick-create/document-field-quick-create.component';
import { DocumentFieldSheetCheckComponent } from './document-field-sheet-check/document-field-sheet-check.component';
import { DocumentTypeEnumComponent } from './document-type-enum/document-type-enum.component';
import { DocumentTypeFormComponent } from './document-type-form/document-type-form.component';
import { DocumentTypePromptComponent } from './document-type-prompt/document-type-prompt.component';

const DOCUMENT_FIELD_ACTIONS = {
    EDIT: 'edit',
    DELETE: 'delete',
} as const;

type DocumentFieldRow = IViewDocumentField & { order: number; ocrLabel?: string };
type DocumentFieldSetting = 'isShow' | 'isChargeable';

// Cột ngắn căn giữa cả tiêu đề lẫn ô; hai cột tên để căn trái cho dễ đọc khi tên dài
const CENTER = 'text-align: center';

@Component({
    selector: 'app-document-type-detail',
    imports: [SharedImports, DialogModule, SelectButtonModule, TabsModule, DataTable, PageHeader, DocumentTypeEnumComponent, DocumentTypePromptComponent],
    templateUrl: './document-type-detail.component.html',
    styleUrl: './document-type-detail.component.scss',
})
export class DocumentTypeDetailComponent extends BaseComponent {
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _documentTypeId = Number(this._activatedRoute.snapshot.paramMap.get('id'));
    private readonly _tabs = viewChild(Tabs);
    private readonly _prompt = viewChild(DocumentTypePromptComponent);

    readonly canUpdateDocumentType = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE);
    readonly canCreateField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_CREATE);
    readonly canUpdateField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_UPDATE);
    readonly canDeleteField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_DELETE);
    readonly canViewEnum = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW);
    readonly canViewPrompt = this._sharedService.isGranted(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_VIEW);

    readonly views = DOCUMENT_TYPE_VIEWS;
    readonly tabs = METADATA_TAB;

    documentType?: IViewDocumentType;
    breadcrumbs: MenuItem[] = [];
    keyword = new FormControl('', { nonNullable: true });
    orderView = new FormControl(DOCUMENT_TYPE_VIEW_TABLE, { nonNullable: true });
    fields: DocumentFieldRow[] = [];
    filteredFields: DocumentFieldRow[] = [];
    selectedFields: DocumentFieldRow[] = [];
    viewHelpVisible = false;
    downloadingTemplate = false;
    // Không có quyền xem tab nào thì luôn ở tab Trường dù URL ghi ?tab=
    private readonly _queryTab = this._activatedRoute.snapshot.queryParamMap.get('tab');
    activeTab: string =
        this._queryTab === METADATA_TAB.PROMPT && this.canViewPrompt
            ? METADATA_TAB.PROMPT
            : this._queryTab === METADATA_TAB.ENUMS && this.canViewEnum
              ? METADATA_TAB.ENUMS
              : METADATA_TAB.FIELDS;

    private readonly _fieldActions: ITableAction[] = [
        ...(this.canUpdateField ? [{ type: DOCUMENT_FIELD_ACTIONS.EDIT, label: 'Sửa', icon: 'pi pi-pencil' }] : []),
        ...(this.canDeleteField ? [{ type: DOCUMENT_FIELD_ACTIONS.DELETE, label: 'Xóa', icon: 'pi pi-trash', severity: 'danger' as const }] : []),
    ];

    readonly columns: IColumn<DocumentFieldRow>[] = [
        { header: 'Thứ tự', field: 'order', headerContainerStyle: `width: 88px; ${CENTER}`, cellStyle: CENTER },
        { header: 'Tên trường', field: 'name', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: DocumentFieldNameCellComponent },
        { header: 'Tên cột import', field: 'fieldImport' },
        {
            header: 'Hiển thị',
            field: 'isShow',
            cellViewType: CellViewTypes.SWITCH,
            headerContainerStyle: `width: 110px; ${CENTER}`,
            cellStyle: CENTER,
            disabled: () => !this.canUpdateField,
        },
        {
            header: 'Tính tiền',
            field: 'isChargeable',
            cellViewType: CellViewTypes.SWITCH,
            headerContainerStyle: `width: 110px; ${CENTER}`,
            cellStyle: CENTER,
            disabled: () => !this.canUpdateField,
        },
        { header: 'OCR', field: 'ocrLabel', headerContainerStyle: CENTER, cellStyle: CENTER },
        ...(this._fieldActions.length
            ? [{ header: 'Thao tác', cellViewType: CellViewTypes.ACTIONS, headerContainerStyle: `width: 100px; ${CENTER}`, cellStyle: CENTER, actions: this._fieldActions }]
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
                    this.router.navigate([METADATA_URL]);
                    return;
                }
                this.documentType = res.data;
                this.breadcrumbs = [{ label: 'Hồ sơ thi công' }, { label: 'Metadata', routerLink: METADATA_URL }, { label: res.data.name }];
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

    // Rời tab Prompt khi form còn thay đổi: p-tabs đã tự chuyển nên trả về tab Prompt, chỉ chuyển thật khi người dùng đồng ý bỏ
    onTabChange(tab: string | number | undefined) {
        const nextTab = String(tab);
        if (nextTab === this.activeTab) return;
        if (!this._prompt()?.isDirty) {
            this.applyTab(nextTab);
            return;
        }

        this._tabs()?.value.set(this.activeTab);
        this.confirmDiscardPrompt().then((accepted) => accepted && this.applyTab(nextTab));
    }

    // Guard rời trang (route metadata/:id) gọi; chỉ hỏi khi tab Prompt còn thay đổi chưa lưu
    canDeactivate(): boolean | Promise<boolean> {
        return this._prompt()?.isDirty ? this.confirmDiscardPrompt() : true;
    }

    // Ghi tab lên URL thay cho lịch sử hiện tại để tải lại trang vẫn ở tab đó mà nút Back không đi qua từng lần đổi tab;
    // về tab Trường thì tải lại để thấy tên danh mục / OCR vừa sửa ở tab khác
    private applyTab(tab: string) {
        this.activeTab = tab;
        if (tab === METADATA_TAB.FIELDS) {
            this.loadFields();
        }
        this.router.navigate([], {
            relativeTo: this._activatedRoute,
            queryParams: { tab: this.activeTab === METADATA_TAB.FIELDS ? null : this.activeTab },
            queryParamsHandling: 'merge',
            replaceUrl: true,
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
            .open(DocumentTypeFormComponent, { header: 'Sửa metadata', width: '32rem', modal: true, closable: true, data: { documentType: this.documentType } })
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
        this.downloadingTemplate = true;
        this._downloadService
            .get(url)
            .pipe(
                finalize(() => (this.downloadingTemplate = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    this.router.navigate([METADATA_URL]);
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

    // Giống confirmAction nhưng trả Promise có cả nhánh từ chối (guard rời trang cần biết người dùng chọn ở lại)
    private confirmDiscardPrompt(): Promise<boolean> {
        return new Promise((resolve) =>
            this._confirmationService.confirm({
                header: 'Bỏ thay đổi chưa lưu?',
                message: 'Prompt OCR còn thay đổi chưa lưu nháp. Rời đi sẽ mất các thay đổi này.',
                closable: true,
                closeOnEscape: true,
                rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
                acceptButtonProps: { label: 'Bỏ thay đổi', severity: 'danger' },
                accept: () => resolve(true),
                reject: () => resolve(false),
            }),
        );
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
