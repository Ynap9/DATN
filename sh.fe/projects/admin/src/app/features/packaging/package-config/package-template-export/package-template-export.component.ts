import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Utils } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { IPackageTypeDropdown } from '@/models/package-config.model';
import { PackageConfigService } from '@/services/package-config.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';

export interface IPackageTemplateExportData {
    packageTypes: IPackageTypeDropdown[];
    packageType: number | null;
}

@Component({
    selector: 'app-package-template-export',
    imports: [SharedImports],
    templateUrl: './package-template-export.component.html',
})
export class PackageTemplateExportComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IPackageTemplateExportData = inject(DynamicDialogConfig).data;
    private readonly _packageConfigService = inject(PackageConfigService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly packageTypes = this._data.packageTypes;
    packageType = this._data.packageType ?? this.packageTypes[0]?.packageType;
    checkedTypes = new Set<number>();
    submitting = false;

    get current() {
        return this.packageTypes.find((item) => item.packageType === this.packageType);
    }

    override ngOnInit(): void {
        this.checkAll();
    }

    // Đổi chuẩn thì tick lại hết loại của chuẩn mới
    selectPackageType(packageType: number) {
        if (packageType === this.packageType) return;
        this.packageType = packageType;
        this.checkAll();
    }

    toggle(metadataType: number) {
        if (!this.checkedTypes.delete(metadataType)) this.checkedTypes.add(metadataType);
    }

    close() {
        this._ref.close();
    }

    onSubmit() {
        const metadataTypes = this.current!.schemas.map((schema) => schema.metadataType).filter((type) => this.checkedTypes.has(type));
        const url = this._packageConfigService.excelTemplateUrl(this.packageType!, metadataTypes);
        this.submitting = true;
        this._downloadService
            .get(url)
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    return;
                }
                Utils.makeDownload(response, url);
                this._ref.close();
            });
    }

    private checkAll() {
        this.checkedTypes = new Set(this.current?.schemas.map((schema) => schema.metadataType));
    }
}
