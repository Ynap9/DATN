import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Utils } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { finalize, Observable } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IProjectAcceptanceSummary, IProjectMyProgress, ITransitionAcceptance, IViewProject, IViewProjectAcceptance } from '@/models/project.model';
import { DocumentTypeService } from '@/services/document-type.service';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUS, PROJECT_STATUSES, PROJECT_URL } from '@/shared/constants/project.constants';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { AcceptanceReasonComponent } from './acceptance-reason/acceptance-reason.component';
import { ProjectFormComponent } from './project-form/project-form.component';
import { ProjectProgressCellComponent } from './project-progress-cell/project-progress-cell.component';

const ACCEPTANCE_START_STATUSES: number[] = [PROJECT_STATUS.DOING];
const ACCEPTANCE_REPORT_STATUSES: number[] = [PROJECT_STATUS.ACCEPTING, PROJECT_STATUS.DONE];

const PROGRESS_ACTIONS = {
    OPEN_SUBMISSIONS: 'openSubmissions',
} as const;

type AcceptanceRow = IProjectAcceptanceSummary & { documentTypeLabel: string };
// link = null: tag chữ thường như cũ
type DocumentTypeTag = { id: number; name: string; link: (string | number)[] | null };

@Component({
    selector: 'app-project-detail',
    imports: [SharedImports, TagModule, DataTable, PageHeader, RouterLink],
    templateUrl: './project-detail.component.html',
})
export class ProjectDetailComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _projectId = Number(this._activatedRoute.snapshot.paramMap.get('id'));

    readonly canUpdate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_UPDATE);
    readonly canAccept = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_ACCEPT);
    readonly canViewSubmissions = this._sharedService.isGranted(PermissionConstants.FUNCTION_SUBMISSION_VIEW);

    project?: IViewProject;
    status?: (typeof PROJECT_STATUSES)[number];
    breadcrumbs: MenuItem[] = [];
    documentTypes: IDocumentTypeDropdown[] = [];
    documentTypeTags: DocumentTypeTag[] = [];
    acceptance?: IViewProjectAcceptance;
    acceptanceRows: AcceptanceRow[] = [];
    acceptanceColumns: IColumn<AcceptanceRow>[] = [];
    submitting = false;

    readonly progressColumns: IColumn<IProjectMyProgress>[] = [
        { header: 'Metadata', field: 'documentTypeName' },
        { header: 'Vai trò', field: 'viewerRole', cellViewType: CellViewTypes.STATUS, statusSeverity: () => 'secondary', headerContainerStyle: 'width: 110px' },
        { header: 'Hộp', field: 'totalBox', headerContainerStyle: 'width: 90px' },
        { header: 'Hồ sơ', field: 'totalProfileRange', headerContainerStyle: 'width: 90px' },
        { header: 'Văn bản', field: 'totalSubmission', headerContainerStyle: 'width: 90px' },
        { header: 'Tiến độ', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: ProjectProgressCellComponent, headerContainerStyle: 'width: 190px' },
        { header: 'Ô đã nhập', field: 'filledFieldCount', headerContainerStyle: 'width: 110px' },
        ...(this.canViewSubmissions
            ? [
                  {
                      header: 'Thao tác',
                      key: 'actions',
                      cellViewType: CellViewTypes.ACTIONS,
                      headerContainerStyle: 'width: 56px; text-align: center',
                      cellStyle: 'text-align: center',
                      actions: [{ type: PROGRESS_ACTIONS.OPEN_SUBMISSIONS, label: 'Mở văn bản', icon: 'pi pi-file' }],
                  },
              ]
            : []),
    ];

    get canStart() {
        return this.canAccept && ACCEPTANCE_START_STATUSES.includes(this.project!.status);
    }

    get canFinish() {
        return this.canAccept && this.project!.status === PROJECT_STATUS.ACCEPTING;
    }

    get canReport() {
        return ACCEPTANCE_REPORT_STATUSES.includes(this.project!.status);
    }

    // Đếm như BE (ProjectAcceptanceBlockers): a = chưa kiểm (gồm trả lại), b = đã kiểm nhưng chưa QA đạt
    get blockerMessage() {
        const total = this.acceptance!.total;
        if (!total.totalSubmissions) return 'Dự án chưa có văn bản nào';
        const notChecked = total.totalSubmissions - total.totalChecked;
        const notPassed = total.totalChecked - total.totalQaPassed;
        return `Còn ${notChecked} văn bản chưa kiểm` + (this.project!.isQaEnabled ? `, ${notPassed} văn bản chưa QA đạt` : '');
    }

    // Chưa đủ điều kiện chỉ Admin được cho phép; dự án chưa có văn bản thì BE chặn cả Admin
    get isTransitionBlocked() {
        return !this.acceptance!.isReady && (!this.acceptance!.canForce || !this.acceptance!.total.totalSubmissions);
    }

    override ngOnInit(): void {
        this._documentTypeService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.documentTypes = res.data;
                    this.mapDocumentTypeTags();
                }
            });
        this.load();
    }

    load() {
        this._projectService
            .findById(this._projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    this.router.navigate([PROJECT_URL]);
                    return;
                }
                this.project = res.data;
                this.status = PROJECT_STATUSES.find((status) => status.value === res.data.status);
                this.breadcrumbs = [{ label: 'Hồ sơ thi công' }, { label: 'Dự án', routerLink: PROJECT_URL }, { label: res.data.name }];
                this.mapDocumentTypeTags();
                if (!this.acceptance) {
                    this.loadAcceptance();
                }
            });
    }

    onProgressAction(event: ITableEvent<IProjectMyProgress>) {
        if (event.type === PROGRESS_ACTIONS.OPEN_SUBMISSIONS) {
            this.router.navigate(this.submissionLink(event.row!.documentTypeId));
        }
    }

    openSheet() {
        window.open(this.project!.urlGoogleSheetSync!, '_blank', 'noopener');
    }

    openForm() {
        this._dialogService
            .open(ProjectFormComponent, {
                header: 'Sửa dự án',
                width: '40rem',
                modal: true,
                closable: true,
                data: { project: this.project, documentTypes: this.documentTypes },
            })
            ?.onClose.subscribe((changed) => changed && this.load());
    }

    startAcceptance() {
        this.confirmTransition('Bắt đầu nghiệm thu', (body) => this._projectService.startAcceptance(this._projectId, body));
    }

    finishAcceptance() {
        this.confirmTransition('Hoàn tất nghiệm thu', (body) => this._projectService.finishAcceptance(this._projectId, body));
    }

    reopenAcceptance() {
        this.openReasonDialog('Trả về đang triển khai', 'Trả dự án về đang triển khai để tiếp tục kiểm / QA.', true, (reason) =>
            this.transition(this._projectService.reopenAcceptance(this._projectId, { reason })),
        );
    }

    downloadReport() {
        const url = this._projectService.acceptanceReportUrl(this._projectId);
        this._downloadService
            .get(url)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    return;
                }
                Utils.makeDownload(response, url);
            });
    }

    private loadAcceptance() {
        this._projectService
            .getAcceptance(this._projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.acceptanceColumns = this.buildAcceptanceColumns(this.project!.isQaEnabled);
                    this.setAcceptance(res.data);
                }
            });
    }

    private confirmTransition(label: string, request: (body: ITransitionAcceptance) => Observable<IBaseResponseWithData<IViewProjectAcceptance>>) {
        if (this.acceptance!.isReady) {
            this.confirmAction({ header: label, message: `${label} dự án ${this.project!.name}?` }, () =>
                this.transition(request({ isForced: false })),
            );
            return;
        }
        this.openReasonDialog(label, `${this.blockerMessage}. Vẫn ${label.toLowerCase()} khi chưa đủ điều kiện?`, false, (reason) =>
            this.transition(request({ isForced: true, reason })),
        );
    }

    private openReasonDialog(header: string, message: string, isRequired: boolean, onConfirm: (reason: string) => void) {
        this._dialogService
            .open(AcceptanceReasonComponent, {
                header,
                width: '32rem',
                modal: true,
                closable: true,
                data: { message, isRequired, confirmLabel: header },
            })
            ?.onClose.subscribe((reason?: string) => reason !== undefined && onConfirm(reason));
    }

    private transition(request$: Observable<IBaseResponseWithData<IViewProjectAcceptance>>) {
        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Cập nhật nghiệm thu thành công')) {
                    this.setAcceptance(res.data);
                    // Tag trạng thái, nút Sửa / nghiệm thu đọc trạng thái từ GET {id}
                    this.load();
                }
            });
    }

    private setAcceptance(acceptance: IViewProjectAcceptance) {
        this.acceptance = acceptance;
        this.acceptanceRows = acceptance.documentTypes.map((row) => ({
            ...row,
            documentTypeLabel: (row.documentTypeName ?? '—') + (row.isRemoved ? ' (đã gỡ)' : ''),
        }));
    }

    private buildAcceptanceColumns(isQaEnabled: boolean): IColumn<AcceptanceRow>[] {
        return [
            { header: 'Metadata', field: 'documentTypeLabel' },
            { header: 'Văn bản', field: 'totalSubmissions', headerContainerStyle: 'width: 90px' },
            { header: 'Chưa kiểm', field: 'totalNotChecked', headerContainerStyle: 'width: 100px' },
            { header: 'Đã kiểm', field: 'totalChecked', headerContainerStyle: 'width: 90px' },
            { header: 'Trả lại', field: 'totalReturned', headerContainerStyle: 'width: 90px' },
            ...(isQaEnabled
                ? [
                      { header: 'QA chưa chấm', field: 'totalQaNotReviewed', headerContainerStyle: 'width: 120px' },
                      { header: 'QA đạt', field: 'totalQaPassed', headerContainerStyle: 'width: 90px' },
                      { header: 'QA lỗi', field: 'totalQaFailed', headerContainerStyle: 'width: 90px' },
                      { header: '% đạt', field: 'qaPassedPercent', headerContainerStyle: 'width: 90px' },
                  ]
                : []),
        ];
    }

    // Đường tới màn danh sách văn bản của một metadata trong dự án
    private submissionLink(documentTypeId: number) {
        return [PROJECT_URL, this._projectId, 'metadata', documentTypeId];
    }

    // Dropdown và chi tiết tải song song; người không phải Admin chỉ thấy tên metadata mình được phân.
    // Người xem hết (myProgresses = null) mở văn bản qua tag ở metadata mình có vai trò; Data / QA mở qua bảng Tiến độ của tôi
    private mapDocumentTypeTags() {
        const ids = this.project?.documentTypeIds ?? [];
        const linkedIds = this.canViewSubmissions && this.project && !this.project.myProgresses
            ? this.project.viewerRoles.map((viewerRole) => viewerRole.documentTypeId)
            : [];
        this.documentTypeTags = this.documentTypes
            .filter((documentType) => ids.includes(documentType.id))
            .map((documentType) => ({
                id: documentType.id,
                name: documentType.name,
                link: linkedIds.includes(documentType.id) ? this.submissionLink(documentType.id) : null,
            }));
    }
}
