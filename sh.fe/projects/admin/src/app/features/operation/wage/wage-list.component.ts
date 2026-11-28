import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Params } from '@angular/router';
import { ShSelectComponent, Utils } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { catchError, debounceTime, finalize, of, Subject, switchMap } from 'rxjs';
import { DownloadService } from '@/core/http/download.service';
import { IViewProjectPair } from '@/models/project.model';
import { IFindWage, IGetListWage, IViewWageSummary } from '@/models/wage.model';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { WageService } from '@/services/wage.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS, MEMBER_VIEWER_ADMIN } from '@/shared/constants/project-member.constants';
import {
    WAGE_ALL_DOCUMENT_TYPES,
    WAGE_PAGE_SIZE,
    WAGE_PAGE_SIZE_OPTIONS,
    WAGE_ROLE_TABS,
    WAGE_VIEW_MODE,
    WageViewMode,
} from '@/shared/constants/wage.constants';
import { IColumn } from '@/shared/models/data-table.models';
import { SharedImports } from '@/shared/import.shared';
import { readErrorResponse } from '@/shared/utils/file-response';
import { WageFormulaCellComponent } from './cells/wage-formula-cell.component';
import { WageMoneyCellComponent } from './cells/wage-money-cell.component';
import { WagePassFailCellComponent } from './cells/wage-pass-fail-cell.component';
import { WagePersonCellComponent } from './cells/wage-person-cell.component';
import { WagePriceCellComponent } from './cells/wage-price-cell.component';
import { WageQuantityCellComponent } from './cells/wage-quantity-cell.component';
import { formatWageNumber, WageRow } from './cells/wage-row';
import { WageSummaryComponent } from './wage-summary/wage-summary.component';

const SEARCH_DEBOUNCE_MS = 400;
const RIGHT_STYLE = 'text-align: right';
const GROUP_ACTUAL = 'Thực nhận';
const GROUP_COMPANY = 'Giá công ty';
const VIEWER_ROLES: string[] = [MEMBER_VIEWER_ADMIN, MEMBER_ROLE.LEADER, MEMBER_ROLE.DATA, MEMBER_ROLE.QA];
const FILTER_ROLES: string[] = [MEMBER_ROLE.DATA, MEMBER_ROLE.QA];

const DESCRIPTIONS = {
    manager: 'Tạm tính theo khối lượng hiện tại và đơn giá mới nhất, tới khi dự án chốt.',
    [MEMBER_ROLE.DATA]: 'Tạm tính tới khi dự án chốt. Văn bản bị thu hồi hoặc trả về kho chung không còn tính cho bạn.',
    [MEMBER_ROLE.QA]: 'Mỗi lượt chấm (Đạt hoặc Không đạt) được tính một lần.',
};

const SCOPE_LABELS: Record<WageViewMode, string> = {
    [WAGE_VIEW_MODE.ADMIN]: 'Toàn bộ thành viên',
    [WAGE_VIEW_MODE.LEADER]: 'Người bạn quản lý',
    [WAGE_VIEW_MODE.STAFF]: 'Chỉ của bạn',
};

interface IWageLoad {
    projectId: number;
    query: IFindWage;
}

@Component({
    selector: 'app-wage-list',
    imports: [SharedImports, DataTable, PageHeader, ShSelectComponent, IconFieldModule, InputIconModule, WageSummaryComponent],
    templateUrl: './wage-list.component.html',
    styleUrl: './wage-list.component.scss',
})
export class WageListComponent extends BaseComponent {
    private readonly _wageService = inject(WageService);
    private readonly _projectService = inject(ProjectService);
    private readonly _sharedService = inject(SharedService);
    private readonly _downloadService = inject(DownloadService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _load$ = new Subject<IWageLoad>();
    private readonly _isAdmin = this._sharedService.isSuperAdmin() || this._sharedService.roles.includes(AuthConstants.ADMIN_ROLE);

    readonly roleTabs = WAGE_ROLE_TABS;
    readonly pageSizeOptions = WAGE_PAGE_SIZE_OPTIONS;
    readonly scopeLabels = SCOPE_LABELS;
    readonly keyword = new FormControl('', { nonNullable: true });
    readonly rowClass = (row: WageRow) =>
        [row.isPriceMissing ? 'wage-row-price-missing' : '', row.isRemoved || !row.isActive ? 'wage-row-inactive' : ''].join(' ').trim();

    // null = chưa tải danh sách cặp
    pairs: IViewProjectPair[] | null = null;
    projects: { projectId: number; projectName: string }[] = [];
    documentTypeOptions: { value: number; label: string }[] = [];
    projectId: number | null = null;
    documentTypeId = WAGE_ALL_DOCUMENT_TYPES;
    role = '';
    pageNumber = this.START_PAGE_NUMBER;
    pageSize = WAGE_PAGE_SIZE;
    viewMode: WageViewMode = WAGE_VIEW_MODE.STAFF;
    staffRole: string = MEMBER_ROLE.DATA;
    rows: WageRow[] = [];
    summary: IViewWageSummary | null = null;
    columns: IColumn<WageRow>[] = [];
    override totalRecords = 0;
    isExporting = false;

    get isAdminView() {
        return this.viewMode === WAGE_VIEW_MODE.ADMIN;
    }

    get isStaff() {
        return this.viewMode === WAGE_VIEW_MODE.STAFF;
    }

    get title() {
        return this.isStaff ? 'Tiền công của bạn' : 'Tiền công';
    }

    get breadcrumbs(): MenuItem[] {
        return [{ label: 'Vận hành' }, { label: 'Tiền công' }];
    }

    get description() {
        return this.isStaff ? DESCRIPTIONS[this.staffRole as keyof typeof DESCRIPTIONS] : DESCRIPTIONS.manager;
    }

    get searchKeyword() {
        return this.isStaff ? '' : this.keyword.value.trim();
    }

    get hasPassFailColumn() {
        return this.columns.some((col) => col.key === 'passFail');
    }

    override ngOnInit(): void {
        this._load$
            .pipe(
                switchMap(({ projectId, query }) => {
                    this.loading = true;
                    return this._wageService.findPaging(projectId, query).pipe(catchError(() => of(null)));
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                this.loading = false;
                if (!res || !this.isResponseSucceed(res)) return;
                this.summary = res.data.summary;
                this.totalRecords = res.data.totalItems;
                this.rows = res.data.items.map((row) => this.toRow(row));
                this.columns = this.buildColumns();
            });
        this.keyword.valueChanges
            .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef))
            .subscribe((keyword) => this.navigate({ keyword: keyword.trim(), pageNumber: null }));
        this.loadPairs();
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với giá trị đang chọn
    onProjectChange(projectId: number) {
        if (projectId === this.projectId) return;
        this.navigate({ projectId, documentTypeId: null, pageNumber: null });
    }

    onDocumentTypeChange(documentTypeId: number | null) {
        const value = documentTypeId ?? WAGE_ALL_DOCUMENT_TYPES;
        if (value === this.documentTypeId) return;
        this.navigate({ documentTypeId: value || null, pageNumber: null });
    }

    selectTab(role: string) {
        if (role === this.role) return;
        this.navigate({ role, pageNumber: null });
    }

    onPageChange(event: PaginatorState) {
        const pageSize = event.rows ?? this.pageSize;
        const pageNumber = (event.page ?? 0) + 1;
        this.navigate({
            pageNumber: pageNumber === this.START_PAGE_NUMBER ? null : pageNumber,
            pageSize: pageSize === WAGE_PAGE_SIZE ? null : pageSize,
        });
    }

    clearKeyword() {
        this.keyword.setValue('');
    }

    exportExcel() {
        if (!this.projectId) return;
        const url = this._wageService.exportExcelUrl(this.projectId, {
            documentTypeId: this.documentTypeId || undefined,
            role: this.role,
            keyword: this.searchKeyword,
        });
        this.isExporting = true;
        this._downloadService
            .get(url)
            .pipe(
                finalize(() => (this.isExporting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe(async (response) => {
                const error = await readErrorResponse(response);
                if (error) {
                    this.isResponseSucceed(error);
                    return;
                }
                Utils.makeDownload(response, url);
            });
    }

    // Lấy mọi cặp một lần (như picker cặp), giữ dự án có ít nhất một cặp mình là Admin / Leader / Data / QA, sắp theo tên
    private loadPairs() {
        this._projectService
            .findPaging({ pageNumber: 1, pageSize: -1 })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                this.pairs = res.data.items.filter((pair) => VIEWER_ROLES.includes(pair.viewerRole));
                const names = new Map(this.pairs.map((pair) => [pair.projectId, pair.projectName]));
                this.projects = [...names]
                    .map(([projectId, projectName]) => ({ projectId, projectName }))
                    .sort((a, b) => a.projectName.localeCompare(b.projectName, 'vi'));
                this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applyUrl());
            });
    }

    // Bộ lọc nằm trên URL (?projectId=&documentTypeId=&role=&keyword=&pageNumber=&pageSize=); thiếu / sai dự án → dự án đầu
    private applyUrl() {
        if (!this.projects.length) return;
        const params = this._activatedRoute.snapshot.queryParamMap;
        const projectId = Number(params.get('projectId'));
        if (!this.projects.some((project) => project.projectId === projectId)) {
            this.navigate({ projectId: this.projects[0].projectId, documentTypeId: null, pageNumber: null });
            return;
        }
        const projectPairs = this.pairs!.filter((pair) => pair.projectId === projectId);
        if (projectId !== this.projectId) {
            this.documentTypeOptions = [
                { value: WAGE_ALL_DOCUMENT_TYPES, label: 'Tất cả metadata' },
                ...projectPairs.map((pair) => ({ value: pair.documentTypeId, label: pair.documentTypeName })),
            ];
        }
        this.projectId = projectId;
        const documentTypeId = Number(params.get('documentTypeId'));
        const pair = projectPairs.find((item) => item.documentTypeId === documentTypeId);
        this.documentTypeId = pair ? documentTypeId : WAGE_ALL_DOCUMENT_TYPES;

        const scopePairs = pair ? [pair] : projectPairs;
        this.viewMode = this._isAdmin
            ? WAGE_VIEW_MODE.ADMIN
            : scopePairs.some((item) => item.viewerRole === MEMBER_ROLE.LEADER)
              ? WAGE_VIEW_MODE.LEADER
              : WAGE_VIEW_MODE.STAFF;
        this.staffRole = scopePairs.every((item) => item.viewerRole === MEMBER_ROLE.QA) ? MEMBER_ROLE.QA : MEMBER_ROLE.DATA;

        const role = params.get('role') ?? '';
        this.role = !this.isStaff && FILTER_ROLES.includes(role) ? role : '';
        const keyword = params.get('keyword') ?? '';
        if (this.keyword.value.trim() !== keyword) {
            this.keyword.setValue(keyword, { emitEvent: false });
        }
        const pageSize = Number(params.get('pageSize'));
        this.pageSize = WAGE_PAGE_SIZE_OPTIONS.includes(pageSize) ? pageSize : WAGE_PAGE_SIZE;
        this.pageNumber = Math.max(this.START_PAGE_NUMBER, Math.floor(Number(params.get('pageNumber'))) || this.START_PAGE_NUMBER);

        // Dựng cột ngay để đổi chế độ / tab không chờ API; giá công ty chốt lại sau khi có summary
        this.columns = this.buildColumns();
        this._load$.next({
            projectId,
            query: {
                documentTypeId: this.documentTypeId || undefined,
                role: this.role,
                keyword: this.searchKeyword,
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
            },
        });
    }

    private toRow(row: IGetListWage): WageRow {
        const isQa = row.role === MEMBER_ROLE.QA;
        return {
            ...row,
            companyPrice: row.companyAmount === null ? undefined : row.companyPrice,
            documentTotal: formatWageNumber(isQa ? row.reviewDocumentCount : row.documentCount),
            priceLink:
                this.viewMode === WAGE_VIEW_MODE.ADMIN ||
                this.pairs!.some((pair) => pair.projectId === this.projectId && pair.documentTypeId === row.documentTypeId && pair.viewerRole === MEMBER_ROLE.LEADER)
                    ? { projectId: this.projectId!, documentTypeId: row.documentTypeId }
                    : null,
        };
    }

    // Cột dựng lại sau mỗi lần tải: theo chế độ, tab (Nhập liệu ẩn cột Đạt / Không đạt), có giá công ty hay không
    private buildColumns(): IColumn<WageRow>[] {
        const roleColumn: IColumn<WageRow> = {
            header: 'Vai trò',
            key: 'role',
            cellViewType: CellViewTypes.BADGE,
            badge: (row) => ({ label: MEMBER_ROLE_LABELS[row.role] ?? row.role, tone: row.role === MEMBER_ROLE.QA ? 'blue' : 'green' }),
        };
        const quantityColumns: IColumn<WageRow>[] = [
            { header: 'Văn bản', field: 'documentTotal', headerContainerStyle: RIGHT_STYLE, cellStyle: RIGHT_STYLE },
            {
                header: 'Khối lượng',
                key: 'quantity',
                cellViewType: CellViewTypes.CUSTOM_COMP,
                customComponent: WageQuantityCellComponent,
                headerContainerStyle: RIGHT_STYLE,
            },
        ];
        const passFailColumn: IColumn<WageRow> = {
            header: 'Đạt / Không đạt',
            key: 'passFail',
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: WagePassFailCellComponent,
            headerContainerStyle: RIGHT_STYLE,
        };
        const priceColumn = (field: 'actualPrice' | 'companyPrice', group?: string): IColumn<WageRow> => ({
            header: 'Đơn giá',
            field,
            group,
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: WagePriceCellComponent,
            headerContainerStyle: RIGHT_STYLE,
        });
        const amountColumn = (field: 'actualAmount' | 'companyAmount', group?: string, cellClass?: string): IColumn<WageRow> => ({
            header: 'Thành tiền',
            field,
            group,
            cellClass,
            cellViewType: CellViewTypes.CUSTOM_COMP,
            customComponent: WageMoneyCellComponent,
            headerContainerStyle: RIGHT_STYLE,
        });

        if (this.isStaff) {
            return [
                { header: 'Metadata', field: 'documentTypeName' },
                roleColumn,
                ...quantityColumns,
                ...(this.staffRole === MEMBER_ROLE.QA ? [passFailColumn] : []),
                priceColumn('actualPrice'),
                { header: 'Cách tính', key: 'formula', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: WageFormulaCellComponent },
                amountColumn('actualAmount', undefined, 'font-semibold'),
            ];
        }
        const companyColumns = this.summary?.companyAmount === null ? [] : [priceColumn('companyPrice', GROUP_COMPANY), amountColumn('companyAmount', GROUP_COMPANY)];
        return [
            { header: 'Người', key: 'person', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: WagePersonCellComponent },
            { header: 'Metadata', field: 'documentTypeName' },
            roleColumn,
            ...quantityColumns,
            ...(this.role === MEMBER_ROLE.DATA ? [] : [passFailColumn]),
            priceColumn('actualPrice', GROUP_ACTUAL),
            amountColumn('actualAmount', GROUP_ACTUAL, 'font-semibold'),
            ...companyColumns,
        ];
    }

    private navigate(change: Params) {
        const queryParams = Object.fromEntries(Object.entries(change).map(([key, value]) => [key, value === '' ? null : value]));
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
    }
}
