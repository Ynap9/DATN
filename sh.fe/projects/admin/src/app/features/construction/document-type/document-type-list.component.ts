import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Utils } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { PaginatorState } from 'primeng/paginator';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { IViewDocumentType } from '@/models/document-type.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { DOCUMENT_TYPE_VIEWS } from '@/shared/constants/document-type.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { IColumn, ITableAction, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { DocumentTypeFormComponent } from './document-type-form/document-type-form.component';

const DOCUMENT_TYPE_ACTIONS = {
    CONFIG_FIELDS: 'configFields',
    EDIT: 'edit',
    DOWNLOAD_TEMPLATE: 'downloadTemplate',
    DELETE: 'delete',
} as const;

type DocumentTypeRow = IViewDocumentType & { defaultViewLabel?: string };

@Component({
    selector: 'app-document-type-list',
    imports: [SharedImports, DataTable, PageHeader],
    templateUrl: './document-type-list.component.html',
})
export class DocumentTypeListComponent extends BaseComponent {
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Hồ sơ thi công' }, { label: 'Loại văn bản' }];
    readonly canCreate = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_TYPE_CREATE);

    keyword = new FormControl('', { nonNullable: true });
    documentTypes: DocumentTypeRow[] = [];
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = 25;
    override totalRecords = 0;

    readonly columns: IColumn<DocumentTypeRow>[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 72px' },
        { header: 'Tên loại văn bản', field: 'name', clickable: true },
        { header: 'Kiểu xem mặc định', field: 'defaultViewLabel', headerContainerStyle: 'width: 200px' },
        {
            header: '',
            cellViewType: CellViewTypes.ACTIONS,
            headerContainerStyle: 'width: 64px',
            actions: [
                { type: DOCUMENT_TYPE_ACTIONS.CONFIG_FIELDS, label: 'Cấu hình trường' },
                ...this.grantedAction(PermissionConstants.FUNCTION_DOCUMENT_TYPE_UPDATE, { type: DOCUMENT_TYPE_ACTIONS.EDIT, label: 'Sửa' }),
                { type: DOCUMENT_TYPE_ACTIONS.DOWNLOAD_TEMPLATE, label: 'Tải template Excel' },
                ...this.grantedAction(PermissionConstants.FUNCTION_DOCUMENT_TYPE_DELETE, { type: DOCUMENT_TYPE_ACTIONS.DELETE, label: 'Xóa' }),
            ],
        },
    ];

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.search());
        this.load();
    }

    load() {
        this.loading = true;
        this._documentTypeService
            .findPaging({ pageNumber: this.pageNumber, pageSize: this.pageSize, keyword: this.keyword.value })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.documentTypes = res.data.items.map((documentType) => ({
                        ...documentType,
                        defaultViewLabel: DOCUMENT_TYPE_VIEWS.find((view) => view.value === documentType.defaultView)?.label,
                    }));
                    this.totalRecords = res.data.totalItems;
                }
            });
    }

    search() {
        this.pageNumber = this.START_PAGE_NUMBER;
        this.load();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.load();
    }

    onAction(event: ITableEvent<DocumentTypeRow>) {
        if (event.type === TableEventTypes.CELL_CLICK || event.type === DOCUMENT_TYPE_ACTIONS.CONFIG_FIELDS) {
            this.router.navigate(['/admin/construction/document-type', event.row!.id]);
        } else if (event.type === DOCUMENT_TYPE_ACTIONS.EDIT) {
            this.openForm(event.row);
        } else if (event.type === DOCUMENT_TYPE_ACTIONS.DOWNLOAD_TEMPLATE) {
            this.downloadTemplate(event.row!);
        } else if (event.type === DOCUMENT_TYPE_ACTIONS.DELETE) {
            this.confirmDeleteDocumentType(event.row!);
        }
    }

    openForm(documentType?: IViewDocumentType) {
        this._dialogService
            .open(DocumentTypeFormComponent, {
                header: documentType ? 'Sửa loại văn bản' : 'Thêm loại văn bản',
                width: '32rem',
                modal: true,
                closable: true,
                data: { documentType },
            })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    private grantedAction(permission: string, action: ITableAction): ITableAction[] {
        return this._sharedService.isGranted(permission) ? [action] : [];
    }

    private downloadTemplate(documentType: IViewDocumentType) {
        const url = this._documentTypeService.excelTemplateUrl(documentType.id);
        this._downloadService
            .get(url)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    this.load();
                    return;
                }
                Utils.makeDownload(response, url);
            });
    }

    private confirmDeleteDocumentType(documentType: IViewDocumentType) {
        this.confirmDelete(
            { header: 'Xóa loại văn bản', message: `Xóa loại văn bản ${documentType.name}? Các trường của loại này không còn dùng được.` },
            () => {
                this._documentTypeService
                    .delete(documentType.id)
                    .pipe(takeUntilDestroyed(this._destroyRef))
                    .subscribe((res) => {
                        if (this.isResponseSucceed(res, true, 'Xóa loại văn bản thành công')) {
                            this.load();
                        }
                    });
            },
        );
    }
}
