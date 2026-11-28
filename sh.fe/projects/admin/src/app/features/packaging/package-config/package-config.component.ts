import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { debounceTime, distinctUntilChanged, finalize, Subject, switchMap } from 'rxjs';
import { IPackageTypeDropdown, IViewPackageSchemaField } from '@/models/package-config.model';
import { PackageConfigService } from '@/services/package-config.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { PACKAGE_FIELD_DATA_TYPES, PACKAGE_FIELD_REQUIREMENT, PACKAGE_FIELD_REQUIREMENTS } from '@/shared/constants/package-config.constants';
import { IColumn } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { IPackageTemplateExportData, PackageTemplateExportComponent } from './package-template-export/package-template-export.component';
import { PackageTextCellComponent } from './package-text-cell/package-text-cell.component';

type PackageFieldRow = IViewPackageSchemaField & { requirementLabel: string; dataTypeLabel: string; codesText: string };

const CENTER = 'text-align: center';

@Component({
    selector: 'app-package-config',
    imports: [SharedImports, ShSelectComponent, DataTable, PageHeader, IconFieldModule, InputIconModule],
    templateUrl: './package-config.component.html',
    styleUrl: './package-config.component.scss',
})
export class PackageConfigComponent extends BaseComponent {
    private readonly _packageConfigService = inject(PackageConfigService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _load$ = new Subject<void>();

    readonly breadcrumbs: MenuItem[] = [{ label: 'Đóng gói' }, { label: 'Cấu hình đóng gói' }];
    readonly keyword = new FormControl('', { nonNullable: true });

    packageTypes: (IPackageTypeDropdown & { label: string })[] = [];
    packageType: number | null = null;
    metadataType: number | null = null;
    rows: PackageFieldRow[] = [];
    override totalRecords = 0;
    // Từ khoá của lần tải đang hiện, để câu rỗng không đổi theo lúc đang gõ
    searchKeyword = '';

    readonly columns: IColumn<PackageFieldRow>[] = [
        { header: 'STT', field: 'sortOrder', headerContainerStyle: `width: 64px; ${CENTER}`, cellStyle: CENTER },
        { header: 'Tên trường', field: 'displayName', headerContainerStyle: CENTER, cellStyle: `${CENTER}; min-width: 220px; font-weight: 500` },
        { header: 'Thẻ EAD', field: 'elementName', headerContainerStyle: CENTER, cellStyle: CENTER, cellClass: 'font-mono text-[13px]' },
        { header: 'Nhóm', field: 'schemaName', headerContainerStyle: CENTER, cellStyle: `${CENTER}; white-space: nowrap` },
        {
            header: 'Bắt buộc',
            field: 'requirementLabel',
            cellViewType: CellViewTypes.STATUS,
            statusSeverity: (row) => PACKAGE_FIELD_REQUIREMENTS.find((item) => item.value === row.requirement)?.severity ?? 'secondary',
            headerContainerStyle: CENTER,
            cellStyle: `${CENTER}; white-space: nowrap`,
        },
        { header: 'Kiểu dữ liệu', field: 'dataTypeLabel', headerContainerStyle: CENTER, cellStyle: `${CENTER}; white-space: nowrap` },
        { header: 'Độ dài', field: 'maxLength', cellViewType: CellViewTypes.NUMBER, headerContainerStyle: CENTER, cellStyle: CENTER },
        { header: 'Danh mục mã', field: 'codesText', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: PackageTextCellComponent, headerContainerStyle: CENTER, cellStyle: CENTER },
        { header: 'Mô tả', field: 'description', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: PackageTextCellComponent, headerContainerStyle: CENTER, cellStyle: CENTER },
    ];

    get currentPackageType() {
        return this.packageTypes.find((item) => item.packageType === this.packageType);
    }

    override ngOnInit(): void {
        this.keyword.valueChanges
            .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this._load$.next());
        // Đổi sheet / chuẩn / từ khoá nhanh thì huỷ request cũ, bảng không bị kết quả cũ ghi đè
        this._load$
            .pipe(
                switchMap(() => {
                    this.loading = true;
                    const keyword = this.keyword.value.trim();
                    return this._packageConfigService
                        .findPaging({ packageType: this.packageType!, metadataType: this.metadataType!, keyword, pageNumber: this.START_PAGE_NUMBER, pageSize: -1 })
                        .pipe(finalize(() => (this.loading = false)));
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                this.searchKeyword = this.keyword.value.trim();
                this.rows = res.data.items.map((field) => this.toRow(field));
                this.totalRecords = res.data.totalItems;
            });
        this.loadPackageTypes();
    }

    loadPackageTypes() {
        this._packageConfigService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                // BE đặt tên kèm mã "Gói hồ sơ nộp (SIP_hoso)": bỏ đuôi mã vì mã đã hiện riêng
                this.packageTypes = res.data.map((item) => {
                    const name = item.name.replace(` (${item.code})`, '');
                    return { ...item, name, label: `${item.code} · ${name}` };
                });
                const first = this.packageTypes[0];
                if (!first) return;
                this.packageType = first.packageType;
                this.metadataType = first.schemas[0]?.metadataType ?? null;
                this._load$.next();
            });
    }

    // Giữ sheet cùng loại metadata nếu chuẩn mới có, không thì về sheet đầu; xoá từ khoá
    onPackageTypeChange(packageType: number) {
        this.packageType = packageType;
        const schemas = this.currentPackageType?.schemas ?? [];
        if (!schemas.some((schema) => schema.metadataType === this.metadataType)) {
            this.metadataType = schemas[0]?.metadataType ?? null;
        }
        this.keyword.setValue('', { emitEvent: false });
        this._load$.next();
    }

    onSheetChange(metadataType: number) {
        if (metadataType === this.metadataType) return;
        this.metadataType = metadataType;
        this._load$.next();
    }

    openExport() {
        const data: IPackageTemplateExportData = { packageTypes: this.packageTypes, packageType: this.packageType };
        this._dialogService.open(PackageTemplateExportComponent, { header: 'Xuất mẫu metadata đóng gói', width: '560px', modal: true, closable: true, data });
    }

    private toRow(field: IViewPackageSchemaField): PackageFieldRow {
        const requirement = PACKAGE_FIELD_REQUIREMENTS.find((item) => item.value === field.requirement);
        const requirementLabel =
            field.requirement === PACKAGE_FIELD_REQUIREMENT.CONDITIONAL
                ? `${requirement?.label}: ${field.conditionElement} = ${(field.conditionValues ?? '').split(',').map((value) => value.trim()).join(', ')}`
                : (requirement?.label ?? '');
        return {
            ...field,
            requirementLabel,
            dataTypeLabel: PACKAGE_FIELD_DATA_TYPES.find((item) => item.value === field.dataType)?.label ?? '',
            codesText: field.codes.map((code) => `${code.code}: ${code.name}`).join(' · '),
        };
    }
}
