import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { MenuItem } from 'primeng/api';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { PaginatorState } from 'primeng/paginator';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TabsModule } from 'primeng/tabs';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { debounceTime, finalize, forkJoin } from 'rxjs';
import { IDocumentFieldDropdown } from '@/models/document-field.model';
import { IViewProject } from '@/models/project.model';
import { ILatestWorkingSubmission, IListQaSubmission, IQaCount, ISubmissionColumn, ISubmissionFilter } from '@/models/submission.model';
import { DocumentFieldService } from '@/services/document-field.service';
import { DocumentTypeService } from '@/services/document-type.service';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DataTable } from '@/shared/components/data-table/data-table.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { TableEventTypes } from '@/shared/constants/data-table.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_URL } from '@/shared/constants/project.constants';
import {
    LATEST_WORKING_TYPE,
    SUBMISSION_CHECK_STATUS,
    SUBMISSION_CHECK_TABS,
    SUBMISSION_ERROR_CODE,
    SUBMISSION_QA_TABS,
    SUBMISSION_ROLE,
    SUBMISSION_ROLE_BY_VIEWER_ROLE,
    SUBMISSION_ROLE_LABELS,
    SUBMISSION_VIEW,
    SubmissionRole,
    SubmissionView,
} from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn, ITableAction, ITableEvent } from '@/shared/models/data-table.models';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { buildCheckColumns, buildQaColumns, SUBMISSION_ACTIONS, SubmissionRow } from './submission-columns';
import { openSubmissionHistory } from './submission-history-dialog/submission-history-dialog.component';
import { readSubmissionQuery, SubmissionQuery } from './submission-query';

const COUNT_PAGE = { pageNumber: 1, pageSize: 1 };
const SEARCH_DEBOUNCE_MS = 400;
const DEFAULT_PAGE_SIZE = 25;

// Đường dẫn màn làm việc dưới project/:id/metadata/:documentTypeId (42.14). Task 43 / 44 khai route rồi điều hướng kèm bộ lọc
const WORK_PATHS = {
    [SUBMISSION_ACTIONS.CHECK]: 'check/:submissionId',
    [SUBMISSION_ACTIONS.QA]: 'qa/:submissionId',
};

// Loại chưa có màn: mở văn bản chỉ báo đang làm
const OPEN_MESSAGES: Record<string, string> = {
    [SUBMISSION_ACTIONS.QA]: 'Màn QA đang làm',
};

// Tên tham số BE của 2 cặp "Trường : giá trị"
const FIELD_PAIR_KEYS = [
    { documentFieldId: 'documentFieldId1', fieldValue: 'fieldValue1', isEmpty: 'isEmpty1' },
    { documentFieldId: 'documentFieldId2', fieldValue: 'fieldValue2', isEmpty: 'isEmpty2' },
] as const;

type FieldPairKeys = (typeof FIELD_PAIR_KEYS)[number];

// Gợi ý giá trị của field-values; isEmpty = mục "(Trống)"
interface IFieldValueOption {
    value: string;
    submissionCount?: number;
    isEmpty?: boolean;
}

interface IFieldPair {
    keys: FieldPairKeys;
    documentFieldId?: number;
    option: IFieldValueOption | null;
    suggestions: IFieldValueOption[];
}

const EMPTY_OPTION: IFieldValueOption = { value: '(Trống)', isEmpty: true };

@Component({
    selector: 'app-submission-list',
    imports: [SharedImports, AutoCompleteModule, IconFieldModule, InputIconModule, TabsModule, TagModule, ToggleSwitchModule, ShSelectComponent, DataTable, PageHeader],
    templateUrl: './submission-list.component.html',
})
export class SubmissionListComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _documentTypeService = inject(DocumentTypeService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _documentFieldService = inject(DocumentFieldService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _projectId = Number(this._activatedRoute.snapshot.paramMap.get('id'));
    private readonly _documentTypeId = Number(this._activatedRoute.snapshot.paramMap.get('documentTypeId'));

    project?: IViewProject;
    // null: không có vai trò ở metadata này (chưa được giao)
    role: SubmissionRole | null = null;
    documentTypeName = '';
    breadcrumbs: MenuItem[] = [];
    query: SubmissionQuery = {};
    counts: Record<string, number> = {};
    // Không có quyền xem trường thì ẩn 2 cặp lọc, giữ ô tìm
    readonly canFilterByField = this._sharedService.isGranted(PermissionConstants.FUNCTION_DOCUMENT_FIELD_VIEW);
    readonly keyword = new FormControl('', { nonNullable: true });
    readonly searchDelay = SEARCH_DEBOUNCE_MS;
    documentFields: IDocumentFieldDropdown[] = [];
    fieldPairs: IFieldPair[] = [];
    rows: (SubmissionRow | IListQaSubmission)[] = [];
    columns: IColumn[] = [];
    override totalRecords = 0;
    // BE trả 820 (chưa được giao văn bản nào) ở paging / count / latest-working
    isNotAssigned = false;
    latestWorking: ILatestWorkingSubmission | null = null;
    // Chế độ của lần tải "Tiếp tục văn bản đang làm" gần nhất: chỉ gọi lại khi đổi chế độ hoặc Làm mới
    private _latestWorkingView?: SubmissionView;

    // Không có vai trò ở metadata (42.3) hoặc BE báo chưa được giao → thay vùng tab + bảng bằng trạng thái trống
    get isUnassigned() {
        return !this.role || this.isNotAssigned;
    }

    get latestWorkingVerb() {
        return this._latestWorkingView === SUBMISSION_VIEW.QA ? 'chấm lúc' : 'lưu lúc';
    }

    get roleLabel() {
        return this.role ? SUBMISSION_ROLE_LABELS[this.role] : '';
    }

    // Chỉ Quản lý đổi được chế độ, và chỉ khi dự án bật QA
    get canToggleQaView() {
        return this.role === SUBMISSION_ROLE.MANAGER && !!this.project?.isQaEnabled;
    }

    get view(): SubmissionView {
        if (this.role === SUBMISSION_ROLE.QA || (this.canToggleQaView && this.query.view === SUBMISSION_VIEW.QA)) {
            return SUBMISSION_VIEW.QA;
        }
        return SUBMISSION_VIEW.CHECK;
    }

    get isQaView() {
        return this.view === SUBMISSION_VIEW.QA;
    }

    get tabs() {
        return this.isQaView ? SUBMISSION_QA_TABS : SUBMISSION_CHECK_TABS;
    }

    get tableKey() {
        return this.isQaView ? `construction-submission-qa-${this._documentTypeId}` : `construction-submission-${this._documentTypeId}`;
    }

    get pageNumber() {
        return this.query.pageNumber ?? this.START_PAGE_NUMBER;
    }

    get pageSize() {
        return this.query.pageSize ?? DEFAULT_PAGE_SIZE;
    }

    // Data → Nhập liệu, QA → QA; Quản lý có cả hai (QA khi dự án bật QA); mọi vai trò có Lịch sử
    get rowActions(): ITableAction[] {
        const check = { type: SUBMISSION_ACTIONS.CHECK, label: 'Nhập liệu', icon: 'pi pi-pencil' };
        const qa = { type: SUBMISSION_ACTIONS.QA, label: 'QA', icon: 'pi pi-check-square' };
        const history = { type: SUBMISSION_ACTIONS.HISTORY, label: 'Lịch sử', icon: 'pi pi-history' };
        if (this.role === SUBMISSION_ROLE.DATA) return [check, history];
        if (this.role === SUBMISSION_ROLE.QA) return [qa, history];
        return [check, ...(this.project!.isQaEnabled ? [qa] : []), history];
    }

    get hasFilter() {
        return !!this.query.keyword || FIELD_PAIR_KEYS.some((keys) => this.query[keys.documentFieldId]);
    }

    // Query không khớp tab nào (vd. chế độ QA chưa chọn tab) thì lấy tab đầu
    get activeTab() {
        return this.tabs.find((tab) => tab.statusCheck === this.query.statusCheck && tab.statusQa === this.query.statusQa) ?? this.tabs[0];
    }

    override ngOnInit(): void {
        this._projectService
            .findById(this._projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                this.project = res.data;
                const viewerRole = res.data.viewerRoles.find((item) => item.documentTypeId === this._documentTypeId);
                this.role = viewerRole ? SUBMISSION_ROLE_BY_VIEWER_ROLE[viewerRole.viewerRole] ?? null : null;
                this.loadDocumentTypeName();
                if (!this.role) {
                    this.resolveMissingRole();
                }
                this.loadDocumentFields();
                this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((params) => {
                    this.query = readSubmissionQuery(params);
                    this.syncFilterControls();
                    this.load();
                    if (this.view !== this._latestWorkingView) {
                        this.loadLatestWorking();
                    }
                });
            });
        this.keyword.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef)).subscribe((value) => {
            const keyword = value.trim() || undefined;
            if (keyword !== this.query.keyword) {
                this.setQuery({ keyword });
            }
        });
    }

    load() {
        if (this.isUnassigned) return;
        this.loadCounts();
        this.loadPage();
    }

    refresh() {
        this.load();
        this.loadLatestWorking();
    }

    onPageChange(event: PaginatorState) {
        this.setQuery({ pageNumber: (event.page ?? 0) + 1, pageSize: event.rows ?? this.pageSize }, false);
    }

    // Bấm Tên file mở theo chế độ đang xem; nút Thao tác mở đúng loại đã chọn, Lịch sử mở dialog
    onAction(event: ITableEvent<SubmissionRow | IListQaSubmission>) {
        const row = event.row!;
        if (event.type === SUBMISSION_ACTIONS.HISTORY) {
            openSubmissionHistory(this._dialogService, row.id, row.fileName);
            return;
        }
        const type = event.type === TableEventTypes.CELL_CLICK ? (this.isQaView ? SUBMISSION_ACTIONS.QA : SUBMISSION_ACTIONS.CHECK) : event.type;
        this.openSubmission(type, row.id);
    }

    backToProject() {
        this.router.navigate([PROJECT_URL]);
    }

    openLatestWorking() {
        this.openSubmission(this._latestWorkingView === SUBMISSION_VIEW.QA ? SUBMISSION_ACTIONS.QA : SUBMISSION_ACTIONS.CHECK, this.latestWorking!.submissionId);
    }

    selectTab(key: string | number | undefined) {
        const tab = this.tabs.find((item) => item.key === key)!;
        this.setQuery({ statusCheck: tab.statusCheck, statusQa: tab.statusQa });
    }

    // Đổi chế độ thì bỏ tab của chế độ cũ
    setQaView(isQaView: boolean) {
        this.setQuery({ view: isQaView ? SUBMISSION_VIEW.QA : undefined, statusCheck: undefined, statusQa: undefined });
    }

    // Đổi trường thì bỏ giá trị / (Trống) cũ của cặp; sh-select phát lại giá trị lúc khởi tạo nên so với state trước
    setField(pair: IFieldPair, documentFieldId: number | null) {
        if ((documentFieldId ?? undefined) !== pair.documentFieldId) {
            this.setQuery(this.pairPatch(pair.keys, documentFieldId ?? undefined));
        }
    }

    // Gợi ý theo chữ đang gõ (keyword của field-values) và các bộ lọc còn lại, bỏ chính cặp này; "(Trống)" luôn đứng đầu
    searchFieldValues(pair: IFieldPair, text: string) {
        const filter = { ...this.buildFilter(), ...this.pairPatch(pair.keys, undefined), statusCheck: this.activeTab.statusCheck, statusQa: this.activeTab.statusQa };
        this._submissionService
            .fieldValues({ ...filter, documentFieldId: pair.documentFieldId!, keyword: text.trim() || undefined })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    pair.suggestions = [EMPTY_OPTION, ...res.data];
                }
            });
    }

    // Chỉ chọn gợi ý (onSelect) mới ghi giá trị — chữ gõ dở không thành bộ lọc; onClear: nút ✕ / xoá hết chữ
    selectFieldValue(pair: IFieldPair, option: IFieldValueOption | null) {
        this.setQuery(this.pairPatch(pair.keys, pair.documentFieldId, option && !option.isEmpty ? option.value : undefined, option?.isEmpty || undefined));
    }

    clearFilters() {
        const patch = FIELD_PAIR_KEYS.reduce((result, keys) => ({ ...result, ...this.pairPatch(keys, undefined) }), {} as SubmissionQuery);
        this.setQuery({ ...patch, keyword: undefined });
    }

    // Nhập liệu: sang màn làm việc kèm bộ lọc + tab đang chọn (bỏ trang, giữ view để nút ← về đúng chế độ); loại khác chưa có màn
    private openSubmission(type: string, submissionId: number) {
        if (type !== SUBMISSION_ACTIONS.CHECK) {
            this._messageService.add({ closable: true, severity: 'info', detail: OPEN_MESSAGES[type], life: 4000 });
            return;
        }
        const { pageNumber, pageSize, ...filter } = this.query;
        const queryParams = { ...filter, statusCheck: this.activeTab.statusCheck, statusQa: this.activeTab.statusQa };
        this.router.navigate([WORK_PATHS[type].replace(':submissionId', String(submissionId))], {
            relativeTo: this._activatedRoute,
            queryParams,
            state: { fromList: true },
        });
    }

    // 820 → trạng thái trống chưa được giao; 303 / 312 / 301 → về danh sách dự án; lỗi khác giữ màn
    private handleError(res: IBaseResponse) {
        if (res.code === SUBMISSION_ERROR_CODE.NOT_ASSIGNED) {
            this.isNotAssigned = true;
            return;
        }
        this.messageError(res.message);
        if (
            res.code === SUBMISSION_ERROR_CODE.PROJECT_NOT_FOUND ||
            res.code === SUBMISSION_ERROR_CODE.DOCUMENT_TYPE_NOT_FOUND ||
            res.code === SUBMISSION_ERROR_CODE.DOCUMENT_TYPE_NOT_IN_PROJECT
        ) {
            this.backToProject();
        }
    }

    // Không có vai trò ở metadata: hỏi BE một lần để tách metadata không thuộc dự án (312 / 301 → về danh sách dự án) với chưa được giao
    private resolveMissingRole() {
        this._submissionService
            .count(this.buildFilter())
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                this.isNotAssigned = true;
            });
    }

    // Data / Quản lý chế độ Nhập liệu lấy văn bản kiểm gần nhất, QA / Quản lý xem theo QA lấy văn bản chấm gần nhất
    private loadLatestWorking() {
        if (this.isUnassigned) return;
        this._latestWorkingView = this.view;
        this._submissionService
            .latestWorking(this._projectId, this._documentTypeId, this.isQaView ? LATEST_WORKING_TYPE.QA : LATEST_WORKING_TYPE.CHECK)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                this.latestWorking = res.data;
            });
    }

    // Mọi thay đổi lọc / tab / chế độ / trang ghi lên URL rồi tải lại từ query string; đổi lọc thì về trang 1
    private setQuery(patch: SubmissionQuery, resetPage = true) {
        const queryParams = Object.fromEntries(
            Object.entries({ ...this.query, ...(resetPage ? { pageNumber: undefined } : {}), ...patch }).filter(
                ([, value]) => value !== undefined && value !== null && value !== '',
            ),
        );
        this.router.navigate([], { relativeTo: this._activatedRoute, queryParams, replaceUrl: true });
    }

    private pairPatch(keys: FieldPairKeys, documentFieldId: number | undefined, fieldValue?: string, isEmpty?: boolean): SubmissionQuery {
        return { [keys.documentFieldId]: documentFieldId, [keys.fieldValue]: fieldValue, [keys.isEmpty]: isEmpty };
    }

    // Đưa query lên ô tìm và 2 cặp lọc (F5 / quay lại vẫn đúng)
    private syncFilterControls() {
        this.keyword.setValue(this.query.keyword ?? '', { emitEvent: false });
        this.fieldPairs = FIELD_PAIR_KEYS.map((keys) => {
            const fieldValue = this.query[keys.fieldValue];
            return {
                keys,
                documentFieldId: this.query[keys.documentFieldId],
                option: this.query[keys.isEmpty] ? EMPTY_OPTION : fieldValue ? { value: fieldValue } : null,
                suggestions: [],
            };
        });
    }

    private loadDocumentFields() {
        if (!this.role || !this.canFilterByField) return;
        this._documentFieldService
            .dropdown(this._documentTypeId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.documentFields = res.data;
                }
            });
    }

    // Bộ lọc chung gửi BE (ô tìm + cặp trường), chưa gồm trạng thái của tab
    private buildFilter(): ISubmissionFilter {
        const { pageNumber, pageSize, view, statusCheck, statusQa, ...filter } = this.query;
        return { ...filter, projectId: this._projectId, documentTypeId: this._documentTypeId };
    }

    // Nhập liệu: cột động dựng lại theo customData mỗi lần paging trả về; QA: cột cố định
    private loadPage() {
        const query = { ...this.buildFilter(), statusCheck: this.activeTab.statusCheck, statusQa: this.activeTab.statusQa, pageNumber: this.pageNumber, pageSize: this.pageSize };
        this.loading = true;
        if (this.isQaView) {
            this.columns = buildQaColumns(this.rowActions);
            this._submissionService
                .qaPaging(query)
                .pipe(finalize(() => (this.loading = false)), takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                    this.rows = res.data.items;
                    this.totalRecords = res.data.totalItems;
                });
            return;
        }
        this._submissionService
            .paging(query)
            .pipe(finalize(() => (this.loading = false)), takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                const fieldColumns: ISubmissionColumn[] = res.data.customData ?? [];
                this.columns = buildCheckColumns(fieldColumns, this.project!.isQaEnabled, this.rowActions);
                this.rows = res.data.items.map((item) => ({
                    ...item,
                    fieldValues: Object.fromEntries(item.submissionDatas.map((submissionData) => [submissionData.documentFieldId, submissionData.data])),
                }));
                this.totalRecords = res.data.totalItems;
            });
    }

    // Nhập liệu: count bỏ trạng thái. QA: Chờ QA đếm trong tập đã kiểm, Đạt / Không đạt đếm trên toàn bộ (customData của qa/paging)
    private loadCounts() {
        if (!this.isQaView) {
            this._submissionService
                .count(this.buildFilter())
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (!this.isResponseSucceed(res, false)) return this.handleError(res);
                    this.counts = { ...res.data };
                });
            return;
        }
        forkJoin([
            this._submissionService.qaPaging({ ...this.buildFilter(), statusCheck: SUBMISSION_CHECK_STATUS.CHECKED, ...COUNT_PAGE }),
            this._submissionService.qaPaging({ ...this.buildFilter(), ...COUNT_PAGE }),
        ])
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(([checked, all]) => {
                const failed = [checked, all].find((res) => !this.isResponseSucceed(res, false));
                if (failed) return this.handleError(failed);
                const checkedCount: IQaCount = checked.data.customData;
                const allCount: IQaCount = all.data.customData;
                this.counts = { totalNotReviewed: checkedCount.totalNotReviewed, totalPassed: allCount.totalPassed, totalFailed: allCount.totalFailed };
            });
    }

    // Data / QA lấy tên từ dòng tiến độ của mình; người xem hết (myProgresses = null) lấy từ dropdown như chi tiết dự án
    private loadDocumentTypeName() {
        const progress = this.project!.myProgresses?.find((item) => item.documentTypeId === this._documentTypeId);
        if (progress) {
            this.setDocumentTypeName(progress.documentTypeName);
            return;
        }
        this._documentTypeService
            .dropdown()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.setDocumentTypeName(res.data.find((documentType) => documentType.id === this._documentTypeId)?.name ?? '');
                }
            });
    }

    private setDocumentTypeName(name: string) {
        this.documentTypeName = name;
        this.breadcrumbs = [
            { label: 'Hồ sơ thi công' },
            { label: 'Dự án', routerLink: PROJECT_URL },
            { label: this.project!.name },
            { label: name },
        ];
    }
}
