import { Location } from '@angular/common';
import { afterNextRender, Component, DestroyRef, ElementRef, inject, Injector } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { SplitterModule, SplitterResizeEndEvent } from 'primeng/splitter';
import { TagModule } from 'primeng/tag';
import { combineLatest, finalize, startWith, Subject, switchMap } from 'rxjs';
import { IViewProject } from '@/models/project.model';
import { ICheckSubmission, IFindSubmissionDetail, ISubmissionDetail, ISubmissionField, ISubmissionFilter } from '@/models/submission.model';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUS, PROJECT_URL } from '@/shared/constants/project.constants';
import {
    SUBMISSION_CHECK_STATUS,
    SUBMISSION_CHECK_STATUSES,
    SUBMISSION_ERROR_CODE,
    SUBMISSION_QA_STATUS,
    SUBMISSION_QA_STATUSES,
    SUBMISSION_ROLE,
    SUBMISSION_ROLE_BY_VIEWER_ROLE,
    SubmissionRole,
} from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { openSubmissionHistory } from './submission-history-dialog/submission-history-dialog.component';
import { SubmissionPdfViewerComponent } from './submission-pdf-viewer/submission-pdf-viewer.component';
import { FONT_SIZE_MAX, FONT_SIZE_MIN, ISubmissionLayout, readSubmissionLayout, writeSubmissionLayout } from './submission-layout';
import { AdjacentDirection, isFilterEdge, readSubmissionQuery, SubmissionQuery } from './submission-query';

const FIELD_ELEMENT_ID_PREFIX = 'submission-field-';
// Lưu bị chặn vì văn bản / dự án vừa đổi trạng thái: tải lại để form chuyển chỉ đọc (43.18)
const RELOAD_AFTER_SAVE_CODES: number[] = [
    SUBMISSION_ERROR_CODE.QA_PASSED_LOCKED,
    SUBMISSION_ERROR_CODE.PROJECT_DONE,
    SUBMISSION_ERROR_CODE.PROJECT_EXPIRED,
];

// Banner chỉ đọc: xám (thiếu quyền / dự án đã xong, hết hạn), xanh (Data mở văn bản đã QA đạt) — 43.9 / 43.11
interface IReadonlyBanner {
    tone: 'gray' | 'green';
    message: string;
}

// Màn Nhập liệu: kiểm tra một văn bản (Task 43)
@Component({
    selector: 'app-submission-check',
    imports: [SharedImports, SplitterModule, TagModule, SubmissionPdfViewerComponent],
    templateUrl: './submission-check.component.html',
    // Phím tắt nghe ở document để vẫn chạy khi focus rơi về body (sau khi đóng hộp hỏi); đóng tab / F5 khi có sửa thì trình duyệt hỏi (43.12)
    host: { '(document:keydown)': 'onKeydown($event)', '(window:beforeunload)': 'onBeforeUnload($event)' },
})
export class SubmissionCheckComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _sharedService = inject(SharedService);
    private readonly _location = inject(Location);
    private readonly _element = inject(ElementRef<HTMLElement>).nativeElement;
    private readonly _injector = inject(Injector);
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _projectId = Number(this._activatedRoute.snapshot.paramMap.get('id'));
    private readonly _documentTypeId = Number(this._activatedRoute.snapshot.paramMap.get('documentTypeId'));
    // Mở từ màn danh sách trong phiên này thì nút ← quay lại đúng tab, lọc, trang (43.13)
    private readonly _fromList = !!(this._location.getState() as { fromList?: boolean } | null)?.fromList;
    private readonly _reload$ = new Subject<void>();
    // Đã hỏi rời văn bản (hoặc vừa lưu xong) thì điều hướng của màn đi qua guard không hỏi lại
    private _allowLeave = false;

    project?: IViewProject;
    role: SubmissionRole | null = null;
    submission?: ISubmissionDetail;
    query: SubmissionQuery = readSubmissionQuery(this._activatedRoute.snapshot.queryParamMap);
    // GET {id} trả 820: văn bản không được giao cho người xem (43.17)
    isNotAssigned = false;
    // Trường hiện trong form: isShowListView, cộng trường QA đánh sai dù ẩn (43.7)
    visibleFields: ISubmissionField[] = [];
    // Trường QA đánh sai còn trong cấu hình (43.15)
    wrongFieldIds = new Set<number>();
    readonlyBanner: IReadonlyBanner | null = null;
    override form = new FormGroup<Record<string, FormControl<string>>>({});
    saving = false;
    navigating = false;
    // Hướng không còn văn bản thì tắt nút, mở văn bản khác thì bật lại
    noMore: Record<AdjacentDirection, boolean> = { previous: false, next: false };
    layout = readSubmissionLayout();
    resizing = false;
    readonly fontSizeMin = FONT_SIZE_MIN;
    readonly fontSizeMax = FONT_SIZE_MAX;

    get checkStatus() {
        return SUBMISSION_CHECK_STATUSES.find((status) => status.value === this.submission?.statusCheck);
    }

    get qaStatus() {
        return SUBMISSION_QA_STATUSES.find((status) => status.value === this.submission?.statusQa);
    }

    // "Hồ sơ … · Hộp …", bỏ vế rỗng
    get locationText() {
        const parts = [
            this.submission?.profileRange ? `Hồ sơ ${this.submission.profileRange}` : '',
            this.submission?.box ? `Hộp ${this.submission.box}` : '',
        ];
        return parts.filter(Boolean).join(' · ');
    }

    // position = null: văn bản không thuộc bộ lọc đang chọn (vd. mở qua "Tiếp tục")
    get isOutsideFilter() {
        return this.submission?.position == null;
    }

    // Banner đỏ chỉ khi văn bản bị QA trả lại và form sửa được (chỉ đọc thắng QA trả lại)
    get isReturned() {
        return !this.readonlyBanner && this.submission?.statusCheck === SUBMISSION_CHECK_STATUS.RETURNED;
    }

    get wrongFields() {
        return this.visibleFields.filter((field) => this.wrongFieldIds.has(field.documentFieldId));
    }

    get isBusy() {
        return this.loading || this.saving || this.navigating;
    }

    // Văn bản đầu / cuối bộ lọc thì tắt sẵn nút chiều đó; văn bản ngoài bộ lọc chỉ tắt sau khi BE báo hết (317)
    isEdge(direction: AdjacentDirection) {
        return this.noMore[direction] || isFilterEdge(this.submission, direction);
    }

    override ngOnInit(): void {
        this._projectService
            .findById(this._projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleProjectError(res);
                this.project = res.data;
                const viewerRole = res.data.viewerRoles.find((item) => item.documentTypeId === this._documentTypeId);
                this.role = viewerRole ? SUBMISSION_ROLE_BY_VIEWER_ROLE[viewerRole.viewerRole] ?? null : null;
                if (this.role === SUBMISSION_ROLE.QA) {
                    this.messageInfo('Màn Nhập liệu không dành cho QA');
                    this.navigateToList();
                    return;
                }
                this.watchSubmission();
            });
    }

    // Có sửa chưa lưu thì hỏi trước khi rời màn (43.12)
    canDeactivate(): boolean | Promise<boolean> {
        return this._allowLeave || !this.hasChanges() ? true : this.confirmLeave();
    }

    // Ctrl + Enter lưu, Ctrl + ↑ / ↓ sang văn bản trước / sau; phím gõ trong dialog (hỏi, Lịch sử) thì bỏ qua
    onKeydown(event: KeyboardEvent) {
        if (!event.ctrlKey || (event.target as HTMLElement).closest('.p-dialog')) return;
        if (event.key === 'Enter') {
            event.preventDefault();
            this.save();
        } else if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
            event.preventDefault();
            this.navigate(event.key === 'ArrowUp' ? 'previous' : 'next');
        }
    }

    onBeforeUnload(event: BeforeUnloadEvent) {
        if (this.hasChanges()) {
            event.preventDefault();
        }
    }

    // ◀ ▶ đi trong bộ lọc (43.8); có sửa chưa lưu thì hỏi trước khi gọi BE
    async navigate(direction: AdjacentDirection) {
        if (!this.submission || this.isBusy || this.isEdge(direction)) return;
        if (this.hasChanges() && !(await this.confirmLeave())) return;
        const request =
            direction === 'next'
                ? this._submissionService.next(this.submission.id, this.buildFilter())
                : this._submissionService.previous(this.submission.id, this.buildFilter());
        this.navigating = true;
        request
            .pipe(
                finalize(() => (this.navigating = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.openSubmission(res.data.id);
                } else if (res.code === SUBMISSION_ERROR_CODE.NO_MORE_SUBMISSION) {
                    this.noMore[direction] = true;
                } else {
                    this.handleProjectError(res);
                }
            });
    }

    // Lưu = Kiểm tra (43.10); Quản lý sửa văn bản đã QA đạt thì hỏi trước vì văn bản phải QA lại
    save() {
        if (!this.submission || this.readonlyBanner || this.isBusy) return;
        if (this.submission.statusQa !== SUBMISSION_QA_STATUS.PASSED || !this.hasChanges()) {
            this.submitCheck();
            return;
        }
        this._confirmationService.confirm({
            header: 'Văn bản đã QA đạt',
            message: 'Lưu thay đổi sẽ đưa văn bản về Chờ QA và phải QA lại. Tiếp tục?',
            closable: true,
            closeOnEscape: true,
            rejectButtonProps: { label: 'Huỷ', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Lưu và QA lại' },
            accept: () => this.submitCheck(),
        });
    }

    backToList() {
        if (this._fromList) {
            this._location.back();
        } else {
            this.navigateToList();
        }
    }

    toggleDirection() {
        this.saveLayout({ ...this.layout, direction: this.layout.direction === 'horizontal' ? 'vertical' : 'horizontal' });
    }

    changeFontSize(step: number) {
        const fontSize = Math.min(FONT_SIZE_MAX, Math.max(FONT_SIZE_MIN, this.layout.fontSize + step));
        this.saveLayout({ ...this.layout, fontSize });
    }

    openHistory() {
        openSubmissionHistory(this._dialogService, this.submission!.id, this.submission!.fileName);
    }

    fieldElementId(documentFieldId: number) {
        return FIELD_ELEMENT_ID_PREFIX + documentFieldId;
    }

    // Khác giá trị gốc sau khi trim (43.12 / 43.15)
    isEdited(field: ISubmissionField) {
        const value = this.form.controls[field.documentFieldId]?.value ?? field.data;
        return value.trim() !== field.data.trim();
    }

    // Bấm chip trường đánh sai / mở văn bản: cuộn tới ô nhập rồi focus
    focusField(documentFieldId: number) {
        const input = this._element.querySelector(`#${this.fieldElementId(documentFieldId)}`) as HTMLInputElement | null;
        input?.scrollIntoView({ block: 'center' });
        input?.focus({ preventScroll: true });
    }

    onResizeEnd(event: SplitterResizeEndEvent) {
        this.resizing = false;
        this.saveLayout({ ...this.layout, sizes: [Number(event.sizes[0]), Number(event.sizes[1])] });
    }

    // :submissionId đổi khi ◀ ▶ mà component không tạo lại; _reload$ tải lại văn bản hiện tại; switchMap bỏ kết quả cũ còn đang tải
    private watchSubmission() {
        combineLatest([this._activatedRoute.paramMap, this._activatedRoute.queryParamMap, this._reload$.pipe(startWith(undefined))])
            .pipe(
                switchMap(([params, queryParams]) => {
                    this.query = readSubmissionQuery(queryParams);
                    this.loading = true;
                    return this._submissionService
                        .findById(Number(params.get('submissionId')), this.detailQuery())
                        .pipe(finalize(() => (this.loading = false)));
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleSubmissionError(res);
                this.isNotAssigned = false;
                this.applySubmission(res.data);
            });
    }

    // Dựng lại form theo văn bản vừa tải; sửa được thì focus trường đầu tiên (43.7)
    private applySubmission(submission: ISubmissionDetail) {
        if (submission.id !== this.submission?.id) {
            this.noMore = { previous: false, next: false };
        }
        this._allowLeave = false;
        this.submission = submission;
        this.readonlyBanner = this.resolveReadonlyBanner(submission);
        const fieldIds = new Set(submission.fields.map((field) => field.documentFieldId));
        this.wrongFieldIds = new Set(submission.qaWrongDocumentFieldIds.filter((id) => fieldIds.has(id)));
        this.visibleFields = submission.fields.filter((field) => field.isShowListView || this.wrongFieldIds.has(field.documentFieldId));
        this.form = new FormGroup(
            Object.fromEntries(this.visibleFields.map((field) => [field.documentFieldId, new FormControl(field.data, { nonNullable: true })])),
        );
        if (this.readonlyBanner) {
            this.form.disable();
        } else if (this.visibleFields.length) {
            afterNextRender(() => this.focusField(this.visibleFields[0].documentFieldId), { injector: this._injector });
        }
    }

    // Xét theo thứ tự, lấy điều kiện đầu tiên khớp (43.9); Quản lý sửa hộ được văn bản đã QA đạt
    private resolveReadonlyBanner(submission: ISubmissionDetail): IReadonlyBanner | null {
        if (!this._sharedService.isGranted(PermissionConstants.FUNCTION_SUBMISSION_CHECK)) {
            return { tone: 'gray', message: 'Bạn chỉ có quyền xem văn bản.' };
        }
        if (!this._sharedService.isSuperAdmin()) {
            if (this.project!.status === PROJECT_STATUS.DONE) return { tone: 'gray', message: 'Dự án đã xong — chỉ xem.' };
            if (this.project!.status === PROJECT_STATUS.EXPIRED) return { tone: 'gray', message: 'Dự án đã hết hạn — chỉ xem.' };
        }
        if (this.role === SUBMISSION_ROLE.DATA && submission.statusQa === SUBMISSION_QA_STATUS.PASSED) {
            return { tone: 'green', message: 'Văn bản đã QA đạt — đã khoá, không sửa được nữa.' };
        }
        return null;
    }

    // Form chỉ đọc không bao giờ "có sửa" (43.12)
    private hasChanges() {
        return !this.readonlyBanner && this.visibleFields.some((field) => this.isEdited(field));
    }

    // Giống confirmAction nhưng trả Promise có cả nhánh ở lại (guard / ◀ ▶ cần biết người dùng chọn gì)
    private confirmLeave(): Promise<boolean> {
        return new Promise((resolve) =>
            this._confirmationService.confirm({
                message: 'Bạn có thay đổi chưa lưu. Rời văn bản này?',
                closable: true,
                closeOnEscape: true,
                rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
                acceptButtonProps: { label: 'Rời đi', severity: 'danger' },
                accept: () => resolve(true),
                reject: () => resolve(false),
            }),
        );
    }

    // Gửi MỌI trường của văn bản: trường hiện lấy giá trị đang gõ, trường ẩn gửi giá trị gốc; không gửi note
    private submitCheck() {
        const submission = this.submission!;
        const body: ICheckSubmission = {
            id: submission.id,
            projectId: this._projectId,
            documentTypeId: this._documentTypeId,
            datas: submission.fields.map((field) => ({
                documentFieldId: field.documentFieldId,
                data: this.form.controls[field.documentFieldId]?.value ?? field.data,
            })),
        };
        this.saving = true;
        this.form.disable();
        this._submissionService
            .check(body)
            .pipe(
                finalize(() => (this.saving = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.form.enable();
                    return this.handleSaveError(res);
                }
                this._messageService.add({ closable: true, severity: 'success', detail: `Đã lưu ${submission.fileName}`, life: 2000 });
                this.openNextAfterSave(submission.id);
            });
    }

    // Lưu xong sang văn bản kế trong bộ lọc; không sang được thì ở lại và tải lại để thấy trạng thái mới
    private openNextAfterSave(submissionId: number) {
        this.navigating = true;
        this._submissionService
            .next(submissionId, this.buildFilter())
            .pipe(
                finalize(() => (this.navigating = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.openSubmission(res.data.id);
                    return;
                }
                if (res.code === SUBMISSION_ERROR_CODE.NO_MORE_SUBMISSION) {
                    this.noMore.next = true;
                } else {
                    this.messageError(res.message);
                }
                this._reload$.next();
            });
    }

    // Đổi văn bản thay cho mục lịch sử hiện tại, giữ bộ lọc; đã hỏi / đã lưu nên guard không hỏi lại
    private openSubmission(submissionId: number) {
        this._allowLeave = true;
        this.router.navigate([PROJECT_URL, this._projectId, 'metadata', this._documentTypeId, 'check', submissionId], {
            queryParams: this.query,
            replaceUrl: true,
        });
    }

    // Mọi lỗi: toast câu BE, giữ giá trị đang gõ; 826 / 316 / 323 → tải lại dự án + văn bản; 820 → trạng thái trống (43.18)
    private handleSaveError(res: IBaseResponse) {
        if (res.code === SUBMISSION_ERROR_CODE.NOT_ASSIGNED) {
            this.handleSubmissionError(res);
            return;
        }
        this.messageError(res.message);
        if (RELOAD_AFTER_SAVE_CODES.includes(res.code)) {
            this.reloadProjectAndSubmission();
        }
    }

    private reloadProjectAndSubmission() {
        this._projectService
            .findById(this._projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleProjectError(res);
                this.project = res.data;
                this._reload$.next();
            });
    }

    private messageInfo(detail: string) {
        this._messageService.add({ closable: true, severity: 'info', detail, life: 4000 });
    }

    private detailQuery(): IFindSubmissionDetail {
        return { ...this.buildFilter(), includePosition: true };
    }

    // Bộ lọc mang theo từ màn danh sách (43.2); view chỉ để quay về đúng chế độ, không gửi BE
    private buildFilter(): ISubmissionFilter {
        const { pageNumber, pageSize, view, ...filter } = this.query;
        return { ...filter, projectId: this._projectId, documentTypeId: this._documentTypeId };
    }

    private navigateToList() {
        this.router.navigate([PROJECT_URL, this._projectId, 'metadata', this._documentTypeId], { queryParams: this.query });
    }

    // 820 → trạng thái trống; 305 / 319 → về màn danh sách; còn lại xử lý như lỗi dự án (43.17)
    private handleSubmissionError(res: IBaseResponse) {
        if (res.code === SUBMISSION_ERROR_CODE.NOT_ASSIGNED) {
            this.submission = undefined;
            this.isNotAssigned = true;
            return;
        }
        if (res.code === SUBMISSION_ERROR_CODE.SUBMISSION_NOT_FOUND || res.code === SUBMISSION_ERROR_CODE.NOT_IN_DOCUMENT_TYPE) {
            this.messageError(res.message);
            this.navigateToList();
            return;
        }
        this.handleProjectError(res);
    }

    // 303 / 301 / 312 → về danh sách dự án như màn danh sách (42.15); lỗi khác giữ màn
    private handleProjectError(res: IBaseResponse) {
        this.messageError(res.message);
        if (
            res.code === SUBMISSION_ERROR_CODE.PROJECT_NOT_FOUND ||
            res.code === SUBMISSION_ERROR_CODE.DOCUMENT_TYPE_NOT_FOUND ||
            res.code === SUBMISSION_ERROR_CODE.DOCUMENT_TYPE_NOT_IN_PROJECT
        ) {
            this.router.navigate([PROJECT_URL]);
        }
    }

    private saveLayout(layout: ISubmissionLayout) {
        this.layout = layout;
        writeSubmissionLayout(layout);
    }
}
