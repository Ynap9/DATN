import { Component, DestroyRef, inject, LOCALE_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ParamMap } from '@angular/router';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SkeletonModule } from 'primeng/skeleton';
import { filter, forkJoin, Observable, Subject, switchMap } from 'rxjs';
import { IViewProject, IViewProjectAcceptance } from '@/models/project.model';
import { IFindStats, IProgressDailyStats, IQaStatsSummary } from '@/models/submission.model';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { getProjectStatusBadge, PROJECT_URL } from '@/shared/constants/project.constants';
import { SUBMISSION_ROLE, SUBMISSION_ROLE_BY_VIEWER_ROLE } from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import {
    bindReportBlock,
    buildRange,
    createReportBlock,
    DEFAULT_REPORT_RANGE,
    formatRangeLabel,
    IReportBlock,
    REPORT_RANGE_OPTIONS,
    ReportRange,
} from './project-report.constants';
import { ReportAcceptanceComponent } from './report-acceptance/report-acceptance.component';
import { ReportDailyChartComponent } from './report-daily-chart/report-daily-chart.component';
import { ReportFieldErrorsComponent } from './report-field-errors/report-field-errors.component';
import { ReportKpiComponent } from './report-kpi/report-kpi.component';
import { ReportProductivityComponent } from './report-productivity/report-productivity.component';
import { ReportQaQualityComponent } from './report-qa-quality/report-qa-quality.component';
import { ReportStatusByMetadataComponent } from './report-status-by-metadata/report-status-by-metadata.component';

// Trạng thái lọc của màn, đọc từ query string
interface IReportQuery {
    documentTypeId: number;
    range: ReportRange;
}

interface IManagedDocumentType {
    id: number;
    name: string;
}

const NOT_MANAGER_MESSAGE = 'Chỉ quản lý dự án xem được báo cáo tiến độ';

@Component({
    selector: 'app-project-report',
    imports: [SharedImports, PageHeader, StatusBadge, ShSelectComponent, SelectButtonModule, SkeletonModule, ReportAcceptanceComponent, ReportKpiComponent, ReportStatusByMetadataComponent, ReportDailyChartComponent, ReportQaQualityComponent, ReportFieldErrorsComponent, ReportProductivityComponent],
    templateUrl: './project-report.component.html',
    styleUrl: './project-report.component.scss',
})
export class ProjectReportComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _locale = inject(LOCALE_ID);

    private readonly _projectId = Number(this._activatedRoute.snapshot.paramMap.get('id'));
    // Mỗi khối một nguồn lọc riêng: đổi lọc phát cho mọi khối, "Thử lại" chỉ phát cho khối đó
    private readonly _dailyFilter$ = new Subject<IFindStats>();
    private readonly _qaSummaryFilter$ = new Subject<IFindStats>();

    readonly canViewProgress = this._sharedService.isGranted(PermissionConstants.FUNCTION_SUBMISSION_PROGRESS_STATS);
    readonly canViewQaStats = this._sharedService.isGranted(PermissionConstants.FUNCTION_SUBMISSION_QA_STATS);
    readonly rangeOptions = REPORT_RANGE_OPTIONS;
    readonly documentTypeControl = new FormControl<number | null>(null);
    readonly rangeControl = new FormControl<ReportRange>(DEFAULT_REPORT_RANGE, { nonNullable: true });

    project?: IViewProject;
    acceptance?: IViewProjectAcceptance;
    breadcrumbs: MenuItem[] = [];
    // Metadata người xem là Admin / Leader, theo thứ tự viewerRoles
    managedDocumentTypes: IManagedDocumentType[] = [];
    query?: IReportQuery;
    statsFilter?: IFindStats;
    daily: IReportBlock<IProgressDailyStats> = createReportBlock();
    qaSummary: IReportBlock<IQaStatsSummary> = createReportBlock();
    updatedAt?: Date;

    get statusBadge() {
        return getProjectStatusBadge(this.project!.status);
    }

    // Dòng nghiệm thu của metadata đang chọn (KPI)
    get selectedSummary() {
        return this.acceptance?.documentTypes.find((row) => row.documentTypeId === this.query?.documentTypeId) ?? null;
    }

    get selectedDocumentTypeName() {
        return this.managedDocumentTypes.find((item) => item.id === this.query?.documentTypeId)?.name ?? '';
    }

    // Dự án tắt QA thì không gọi các API qa-stats
    get canLoadQaStats() {
        return this.canViewQaStats && !!this.project?.isQaEnabled;
    }

    // Nhãn khoảng lấy từ ngày BE đã chuẩn hoá trả về
    get rangeLabel() {
        const stats = this.daily.data ?? this.qaSummary.data;
        return stats ? formatRangeLabel(stats.fromDate, stats.toDate, this._locale) : '';
    }

    override ngOnInit(): void {
        this.bindBlock(this.daily, this._dailyFilter$, (query) => this._submissionService.progressDailyStats(query));
        this.bindBlock(this.qaSummary, this._qaSummaryFilter$, (query) => this._submissionService.qaStatsSummary(query));
        this.loadContext()
            .pipe(
                filter(([projectRes, acceptanceRes]) => this.applyContext(projectRes, acceptanceRes)),
                switchMap(() => this._activatedRoute.queryParamMap),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((params) => this.applyQuery(params));
        this.documentTypeControl.valueChanges
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((documentTypeId) => documentTypeId && this.setQuery({ documentTypeId }));
        this.rangeControl.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((range) => this.setQuery({ range }));
    }

    refresh() {
        this.reloadContext();
        this.loadStats();
    }

    // Tải lại dự án + nghiệm thu (Làm mới, hoặc sau khi chuyển trạng thái nghiệm thu)
    reloadContext() {
        this.loadContext()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(([projectRes, acceptanceRes]) => this.applyContext(projectRes, acceptanceRes));
    }

    retryDaily() {
        this._dailyFilter$.next(this.statsFilter!);
    }

    retryQaSummary() {
        this._qaSummaryFilter$.next(this.statsFilter!);
    }

    private loadContext() {
        return forkJoin([this._projectService.findById(this._projectId), this._projectService.getAcceptance(this._projectId)]);
    }

    // Lỗi (303...) hoặc người xem không quản lý metadata nào → toast rồi về danh sách dự án
    private applyContext(projectRes: IBaseResponseWithData<IViewProject>, acceptanceRes: IBaseResponseWithData<IViewProjectAcceptance>) {
        if (!this.isResponseSucceed(projectRes) || !this.isResponseSucceed(acceptanceRes)) {
            this.router.navigate([PROJECT_URL]);
            return false;
        }
        const names = new Map(acceptanceRes.data.documentTypes.map((row) => [row.documentTypeId, row.documentTypeName]));
        this.managedDocumentTypes = projectRes.data.viewerRoles
            .filter((viewerRole) => SUBMISSION_ROLE_BY_VIEWER_ROLE[viewerRole.viewerRole] === SUBMISSION_ROLE.MANAGER)
            .map((viewerRole) => ({ id: viewerRole.documentTypeId, name: names.get(viewerRole.documentTypeId) ?? '—' }));
        if (!this.managedDocumentTypes.length) {
            this.messageWarning(NOT_MANAGER_MESSAGE);
            this.router.navigate([PROJECT_URL]);
            return false;
        }
        this.project = projectRes.data;
        this.acceptance = acceptanceRes.data;
        this.breadcrumbs = [
            { label: 'Hồ sơ thi công' },
            { label: 'Dự án', routerLink: PROJECT_URL },
            { label: projectRes.data.name },
            { label: 'Báo cáo tiến độ' },
        ];
        this.updatedAt = new Date();
        return true;
    }

    // Thiếu / sai giá trị thì về mặc định: metadata quản lý đầu tiên, 30 ngày
    private applyQuery(params: ParamMap) {
        const documentTypeId = Number(params.get('documentTypeId'));
        const range = REPORT_RANGE_OPTIONS.find((option) => option.value === params.get('range'))?.value ?? DEFAULT_REPORT_RANGE;
        this.query = {
            documentTypeId: this.managedDocumentTypes.some((item) => item.id === documentTypeId) ? documentTypeId : this.managedDocumentTypes[0].id,
            range,
        };
        // Thiếu / sai tham số: ghi lọc mặc định lên URL rồi đợi lượt phát kế tiếp mới tải, tránh gọi số hai lần
        if (params.get('documentTypeId') !== String(this.query.documentTypeId) || params.get('range') !== range) {
            this.setQuery({});
            return;
        }
        this.documentTypeControl.setValue(this.query.documentTypeId, { emitEvent: false });
        this.rangeControl.setValue(this.query.range, { emitEvent: false });
        this.loadStats();
    }

    private setQuery(patch: Partial<IReportQuery>) {
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams: { ...this.query, ...patch }, replaceUrl: true });
    }

    private loadStats() {
        this.statsFilter = {
            projectId: this._projectId,
            documentTypeId: this.query!.documentTypeId,
            ...buildRange(this.query!.range, this.project!.startedAt, new Date()),
        };
        if (this.canViewProgress) {
            this._dailyFilter$.next(this.statsFilter);
        }
        if (this.canLoadQaStats) {
            this._qaSummaryFilter$.next(this.statsFilter);
        }
    }

    // Khối trên màn đầu: xong thì cập nhật "Cập nhật lúc"
    private bindBlock<T>(block: IReportBlock<T>, filter$: Subject<IFindStats>, request: (query: IFindStats) => Observable<IBaseResponseWithData<T>>) {
        bindReportBlock(block, filter$, request, (res) => this.isResponseSucceed(res), this._destroyRef, () => (this.updatedAt = new Date()));
    }
}
