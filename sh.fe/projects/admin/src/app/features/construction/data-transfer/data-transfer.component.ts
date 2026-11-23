import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MenuItem } from 'primeng/api';
import { IViewProjectPair } from '@/models/project.model';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { ProjectPairPicker } from '@/shared/components/project-pair-picker/project-pair-picker.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { MEMBER_ROLE, MEMBER_VIEWER_ADMIN } from '@/shared/constants/project-member.constants';
import { PROJECT_STATUS, PROJECT_STATUSES } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';
import { formatNumber } from './data-transfer.constants';
import { ExportPanelComponent } from './export-panel/export-panel.component';
import { ImportWizardComponent } from './import-wizard/import-wizard.component';

const DATA_TRANSFER_TAB = {
    IMPORT: 'import',
    EXPORT: 'export',
} as const;

type DataTransferTab = (typeof DATA_TRANSFER_TAB)[keyof typeof DATA_TRANSFER_TAB];

// Mỗi tab chỉ hiện khi có quyền tương ứng (48.1)
const TABS: { key: DataTransferTab; label: string; permission: string }[] = [
    { key: DATA_TRANSFER_TAB.IMPORT, label: 'Nhập dữ liệu', permission: PermissionConstants.FUNCTION_SUBMISSION_IMPORT },
    { key: DATA_TRANSFER_TAB.EXPORT, label: 'Xuất Excel', permission: PermissionConstants.FUNCTION_SUBMISSION_EXPORT },
];

// Trạng thái dự án BE chặn import: 341 / 313 / 323 (48.6)
const NOT_IMPORTABLE_STATUS_TEXT: Record<number, string> = {
    [PROJECT_STATUS.ACCEPTING]: 'đang nghiệm thu',
    [PROJECT_STATUS.DONE]: 'đã xong',
    [PROJECT_STATUS.EXPIRED]: 'đã hết hạn',
};

// Màn Nhập / Xuất dữ liệu: import Google Sheet / Excel và xuất Excel theo cặp dự án – loại tài liệu (Task 48)
@Component({
    selector: 'app-data-transfer',
    imports: [SharedImports, PageHeader, ProjectPairPicker, ImportWizardComponent, ExportPanelComponent],
    templateUrl: './data-transfer.component.html',
})
export class DataTransferComponent extends BaseComponent {
    private readonly _sharedService = inject(SharedService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _importWizard = viewChild(ImportWizardComponent);
    private readonly _pairPicker = viewChild(ProjectPairPicker);

    readonly tabKey = DATA_TRANSFER_TAB;
    readonly breadcrumbs: MenuItem[] = [{ label: 'Hồ sơ thi công' }, { label: 'Nhập / Xuất dữ liệu' }];
    // Chỉ Admin / Leader của dự án được import / xuất, người khác BE trả 337
    readonly viewerRoles = [MEMBER_VIEWER_ADMIN, MEMBER_ROLE.LEADER];
    readonly tabs = TABS.filter((tab) => this._sharedService.isGranted(tab.permission));
    readonly hasImportTab = this.tabs.some((tab) => tab.key === DATA_TRANSFER_TAB.IMPORT);
    readonly hasExportTab = this.tabs.some((tab) => tab.key === DATA_TRANSFER_TAB.EXPORT);
    // Guard đảm bảo có ít nhất một quyền nên luôn có tab
    activeTab: DataTransferTab = this.tabs[0].key;

    // null = picker chưa tải xong danh sách cặp
    pairs: IViewProjectPair[] | null = null;
    pair: IViewProjectPair | null = null;
    totalSubmissions: number | null = null;
    // Đang import thì khoá tab (48.4)
    isImporting = false;

    // "<trạng thái dự án> · <N> văn bản" bên phải hàng tab (48.3)
    get pairSummary() {
        const status = PROJECT_STATUSES.find((item) => item.value === this.pair?.status)?.label.toLocaleLowerCase('vi') ?? '';
        const total = this.totalSubmissions === null ? '…' : formatNumber(this.totalSubmissions);
        return `Dự án ${status} · ${total} văn bản`;
    }

    get notImportableMessage() {
        const statusText = this.pair ? NOT_IMPORTABLE_STATUS_TEXT[this.pair.status] : undefined;
        return statusText ? `Dự án ${statusText} — không import được.` : null;
    }

    onPairChange(pair: IViewProjectPair | null) {
        this.pair = pair;
        this.totalSubmissions = null;
        if (pair) {
            this.loadTotalSubmissions(pair);
        }
    }

    selectTab(tab: DataTransferTab) {
        if (!this.isImporting) this.activeTab = tab;
    }

    // Không còn quản lý dự án (337) → tải lại danh sách cặp
    reloadPairs() {
        this._pairPicker()?.reload();
    }

    onImported() {
        if (this.pair) this.loadTotalSubmissions(this.pair);
    }

    // Rời màn khi đang import Google (đồng bộ) thì hỏi trước (48.13)
    canDeactivate(): boolean | Promise<boolean> {
        return this._importWizard()?.isLeaveBlocked ? this.confirmLeave() : true;
    }

    private confirmLeave(): Promise<boolean> {
        return new Promise((resolve) =>
            this._confirmationService.confirm({
                message: 'Import đang chạy, rời trang có thể không biết kết quả. Vẫn rời?',
                closable: true,
                closeOnEscape: true,
                rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
                acceptButtonProps: { label: 'Rời đi', severity: 'danger' },
                accept: () => resolve(true),
                reject: () => resolve(false),
            }),
        );
    }

    // Số văn bản của cặp; đổi cặp nhanh thì bỏ kết quả của cặp cũ về sau
    private loadTotalSubmissions(pair: IViewProjectPair) {
        this._submissionService
            .count({ projectId: pair.projectId, documentTypeId: pair.documentTypeId })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (pair !== this.pair || !this.isResponseSucceed(res)) return;
                this.totalSubmissions = res.data.total;
            });
    }
}
