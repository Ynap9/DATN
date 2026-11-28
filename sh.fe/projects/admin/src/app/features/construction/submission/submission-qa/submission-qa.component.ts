import { formatDate, Location } from '@angular/common';
import { Component, DestroyRef, ElementRef, inject, LOCALE_ID } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { SplitterModule, SplitterResizeEndEvent } from 'primeng/splitter';
import { TagModule } from 'primeng/tag';
import { catchError, combineLatest, finalize, forkJoin, map, of, startWith, Subject, switchMap } from 'rxjs';
import { IViewProject } from '@/models/project.model';
import {
    IFindSubmissionDetail,
    IQaSubmissionDetail,
    IReviewQaSubmission,
    ISubmissionField,
    ISubmissionFilter,
    ISubmissionQaLog,
} from '@/models/submission.model';
import { ProjectService } from '@/services/project.service';
import { SharedService } from '@/services/shared.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { DOCUMENT_ENUM_OUT_OF_TEXT } from '@/shared/constants/document-field.constants';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_STATUS, PROJECT_URL } from '@/shared/constants/project.constants';
import {
    QA_QUICK_REASONS,
    QA_REASON_MAX_LENGTH,
    QA_RESULT,
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
import { isOutOfEnum } from '@/shared/utils/enum-value';
import { openSubmissionHistory } from '../submission-history-dialog/submission-history-dialog.component';
import { FONT_SIZE_MAX, FONT_SIZE_MIN, ISubmissionLayout, readSubmissionLayout, writeSubmissionLayout } from '../submission-layout';
import { SubmissionPdfCacheService } from '../submission-pdf-viewer/submission-pdf-cache.service';
import { SubmissionPdfViewerComponent } from '../submission-pdf-viewer/submission-pdf-viewer.component';
import { AdjacentDirection, DOCUMENT_TYPE_NAME_PARAM, isFilterEdge, readSubmissionQuery, SubmissionQuery } from '../submission-query';
import { buildLocationText } from '../submission-location';

const BANNER_DATE_FORMAT = 'HH:mm dd/MM/yyyy';
const REASON_INPUT_ID = 'submission-qa-reason';
// Gửi bị chặn vì văn bản / phiếu / dự án vừa đổi trạng thái: tải lại để màn chuyển chỉ xem nếu cần (44.18)
const RELOAD_AFTER_REVIEW_CODES: number[] = [
    SUBMISSION_ERROR_CODE.PROJECT_DONE,
    SUBMISSION_ERROR_CODE.PROJECT_EXPIRED,
    SUBMISSION_ERROR_CODE.NOT_CHECKED_FOR_QA,
    SUBMISSION_ERROR_CODE.QA_SUBMISSION_LOCKED,
    SUBMISSION_ERROR_CODE.QA_NOT_ASSIGNEE,
    SUBMISSION_ERROR_CODE.PROJECT_QA_DISABLED,
];

type BannerTone = 'gray' | 'green' | 'red' | 'amber';

// Banner đầu khu phải (44.10): title in đậm, message dòng thường; mỗi lúc một banner.
// isFinished: văn bản chưa kiểm / đã trả / đã đạt — đáy khu phải hiện hàng "Văn bản sau" thay thanh quyết định (44.9)
interface IQaBanner {
    tone: BannerTone;
    title?: string;
    message?: string;
    isFinished?: boolean;
}

// Xám: chỉ xem; xanh: đã đạt; đỏ: đã trả lại; vàng: lần trước sai
const BANNER_STYLES: Record<BannerTone, { box: string; icon: string }> = {
    gray: { box: 'border-gray-200 bg-gray-50', icon: 'pi-lock text-gray-500' },
    green: { box: 'border-green-200 bg-green-50', icon: 'pi-check-circle text-green-600' },
    red: { box: 'border-red-200 bg-red-50', icon: 'pi-exclamation-triangle text-red-600' },
    amber: { box: 'border-amber-200 bg-amber-50 text-amber-900', icon: 'pi-history text-amber-700' },
};

// Màn QA: chấm Đạt / Trả lại một văn bản (Task 44)
@Component({
    selector: 'app-submission-qa',
    imports: [SharedImports, SplitterModule, TagModule, ShInputTextComponent, SubmissionPdfViewerComponent],
    templateUrl: './submission-qa.component.html',
    // Phím tắt nghe ở document để vẫn chạy khi focus rơi về body; đóng tab / F5 khi đang chấm dở thì trình duyệt hỏi (44.13)
    host: { '(document:keydown)': 'onKeydown($event)', '(window:beforeunload)': 'onBeforeUnload($event)' },
    providers: [SubmissionPdfCacheService],
})
export class SubmissionQaComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _sharedService = inject(SharedService);
    private readonly _location = inject(Location);
    private readonly _locale = inject(LOCALE_ID);
    private readonly _element = inject(ElementRef<HTMLElement>).nativeElement;
    private readonly _destroyRef = inject(DestroyRef);

    private readonly _projectId = Number(this._activatedRoute.snapshot.paramMap.get('id'));
    private readonly _documentTypeId = Number(this._activatedRoute.snapshot.paramMap.get('documentTypeId'));
    // Tên metadata màn danh sách gửi kèm; mở link trực tiếp thì rỗng
    private readonly _documentTypeName = this._activatedRoute.snapshot.queryParamMap.get(DOCUMENT_TYPE_NAME_PARAM) ?? '';
    // Mở từ màn danh sách trong phiên này thì nút ← quay lại đúng tab, lọc, trang (44.14)
    private readonly _fromList = !!(this._location.getState() as { fromList?: boolean } | null)?.fromList;
    private readonly _reload$ = new Subject<void>();
    // Đã hỏi bỏ kết quả đang chấm thì điều hướng của màn đi qua guard không hỏi lại
    private _allowLeave = false;

    project?: IViewProject;
    role: SubmissionRole | null = null;
    // viewerRole BE = Leader: chỉ xem kết quả QA (44.9)
    isLeader = false;
    submission?: IQaSubmissionDetail;
    // Lượt QA mới → cũ; null khi GET qa-logs lỗi: bỏ lượt QA ở dải ngữ cảnh, không chặn chấm (44.17)
    qaLogs: ISubmissionQaLog[] | null = null;
    query: SubmissionQuery = readSubmissionQuery(this._activatedRoute.snapshot.queryParamMap);
    // GET {id}/qa trả 820: văn bản không được giao cho người xem (44.17)
    isNotAssigned = false;
    readonlyBanner: IQaBanner | null = null;
    // Trường hiện: isShowListView, cộng trường "Lần trước sai" dù ẩn; thứ tự BE trả (44.7)
    visibleFields: ISubmissionField[] = [];
    readonly isOutOfEnum = isOutOfEnum;
    readonly outOfEnumText = DOCUMENT_ENUM_OUT_OF_TEXT;
    // Trường QA đang bấm "Sai"
    markedFieldIds = new Set<number>();
    // Văn bản đã trả lại: trường QA đánh sai tô như đang đánh dấu, không có nút (44.9 e)
    returnedFieldIds = new Set<number>();
    // Lượt Không đạt mới nhất khi văn bản quay lại Chờ QA (44.12)
    previousWrongLog: ISubmissionQaLog | null = null;
    previousWrongFieldIds = new Set<number>();
    // Lý do trả lại: giữ khi bỏ hết đánh dấu, chỉ xoá khi "Bỏ đánh dấu, chấm Đạt" / gửi xong / đổi văn bản (44.8)
    readonly reasonControl = new FormControl('', { nonNullable: true });
    submitting = false;
    navigating = false;
    // Hướng không còn văn bản thì tắt nút, mở văn bản khác thì bật lại
    noMore: Record<AdjacentDirection, boolean> = { previous: false, next: false };
    layout = readSubmissionLayout();
    resizing = false;
    readonly fontSizeMin = FONT_SIZE_MIN;
    readonly fontSizeMax = FONT_SIZE_MAX;
    readonly reasonMaxLength = String(QA_REASON_MAX_LENGTH);
    readonly quickReasons = QA_QUICK_REASONS;
    readonly reasonInputId = REASON_INPUT_ID;

    get checkStatus() {
        return SUBMISSION_CHECK_STATUSES.find((status) => status.value === this.submission?.statusCheck);
    }

    get qaStatus() {
        return SUBMISSION_QA_STATUSES.find((status) => status.value === this.submission?.statusQa);
    }

    // "Hồ sơ … · Hộp … · <metadata>", bỏ vế rỗng; dữ liệu đã có tiền tố thì không lặp
    get locationText() {
        return buildLocationText(this.submission?.profileRange, this.submission?.box, this._documentTypeName);
    }

    // position = null: văn bản không thuộc bộ lọc đang chọn (vd. mở qua "Tiếp tục")
    get isOutsideFilter() {
        return this.submission?.position == null;
    }

    get isWaitingQa() {
        return this.submission?.statusCheck === SUBMISSION_CHECK_STATUS.CHECKED && this.submission.statusQa === SUBMISSION_QA_STATUS.NOT_REVIEWED;
    }

    // Số lần bị trả = số lượt QA Không đạt
    get returnedCount() {
        return this.qaLogs?.filter((log) => log.newStatusQa === SUBMISSION_QA_STATUS.FAILED).length ?? 0;
    }

    // "Lần k (bị trả n lần)": đang Chờ QA thì lượt này chưa có log nên k = số log + 1 (44.6)
    get qaTurnText() {
        if (!this.qaLogs) return '—';
        const turn = this.qaLogs.length + (this.isWaitingQa ? 1 : 0);
        if (!turn) return '—';
        return this.returnedCount ? `Lần ${turn} (bị trả ${this.returnedCount} lần)` : `Lần ${turn}`;
    }

    get canReview() {
        return !this.readonlyBanner;
    }

    // Chỉ xem thắng banner vàng (44.10)
    get banner(): IQaBanner | null {
        if (this.readonlyBanner) return this.readonlyBanner;
        if (!this.previousWrongLog) return null;
        const reason = this.previousWrongLog.reason?.trim();
        return {
            tone: 'amber',
            title: `Văn bản đã bị trả ${this.returnedCount} lần — kiểm trước ${this.previousWrongFieldIds.size} trường gắn “Lần trước sai”.`,
            message: reason ? `Lý do lần trước: “${reason}”` : undefined,
        };
    }

    get bannerStyle() {
        return BANNER_STYLES[this.banner!.tone];
    }

    // Trường đang đánh dấu theo thứ tự hiện
    get markedFields() {
        return this.visibleFields.filter((field) => this.markedFieldIds.has(field.documentFieldId));
    }

    get markedFieldNames() {
        return this.markedFields.map((field) => field.name).join(', ');
    }

    get trimmedReason() {
        return this.reasonControl.value.trim();
    }

    get isBusy() {
        return this.loading || this.submitting || this.navigating;
    }

    // Chấm dở: có trường đánh dấu hoặc lý do khác rỗng; chỉ xem không bao giờ chấm dở (44.13)
    get isDirty() {
        return this.canReview && (this.markedFieldIds.size > 0 || !!this.trimmedReason);
    }

    // Văn bản đầu / cuối bộ lọc thì tắt sẵn nút chiều đó; văn bản ngoài bộ lọc chỉ tắt sau khi BE báo hết (317)
    isEdge(direction: AdjacentDirection) {
        return this.noMore[direction] || isFilterEdge(this.submission, direction);
    }

    // Đang đánh dấu, hoặc trường QA đã đánh sai của văn bản đã trả lại
    isMarked(field: ISubmissionField) {
        return this.markedFieldIds.has(field.documentFieldId) || this.returnedFieldIds.has(field.documentFieldId);
    }

    // Kiểu vàng chỉ khi QA chưa bấm "Sai" trường đó (44.12)
    isPreviousWrong(field: ISubmissionField) {
        return this.previousWrongFieldIds.has(field.documentFieldId) && !this.isMarked(field);
    }

    toggleMark(field: ISubmissionField) {
        if (!this.canReview || this.isBusy) return;
        if (this.markedFieldIds.has(field.documentFieldId)) {
            this.markedFieldIds.delete(field.documentFieldId);
        } else {
            this.markedFieldIds.add(field.documentFieldId);
        }
    }

    // Nối chip vào cuối lý do, cắt phần vượt giới hạn rồi focus lại ô nhập (44.15)
    addQuickReason(chip: string) {
        const reason = this.trimmedReason ? `${this.trimmedReason} ${chip}.` : `${chip}.`;
        this.reasonControl.setValue(reason.slice(0, QA_REASON_MAX_LENGTH));
        this.focusReason();
    }

    // Về thanh Đạt: bỏ mọi đánh dấu và cả lý do
    clearMarks() {
        this.markedFieldIds = new Set();
        this.reasonControl.reset();
    }

    // Đạt khi không đánh dấu trường nào, ngược lại Trả lại; Trả lại thiếu lý do thì chỉ focus ô nhập (44.16)
    submitReview() {
        if (!this.submission || !this.canReview || this.isBusy) return;
        if (this.markedFieldIds.size && !this.trimmedReason) {
            this.focusReason();
            return;
        }
        const submission = this.submission;
        const isPassed = !this.markedFieldIds.size;
        const body: IReviewQaSubmission = {
            id: submission.id,
            projectId: this._projectId,
            documentTypeId: this._documentTypeId,
            result: isPassed ? QA_RESULT.PASSED : QA_RESULT.FAILED,
            reason: isPassed ? null : this.trimmedReason,
            wrongDocumentFieldIds: this.markedFields.map((field) => field.documentFieldId),
        };
        this.submitting = true;
        this.reasonControl.disable();
        this._submissionService
            .reviewQa(body)
            .pipe(
                finalize(() => {
                    this.submitting = false;
                    this.reasonControl.enable();
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) return this.handleReviewError(res);
                const detail = isPassed ? `Đã chấm Đạt ${submission.fileName}` : `Đã trả lại ${submission.fileName}`;
                this._messageService.add({ closable: true, severity: 'success', detail, life: 2000 });
                this.clearMarks();
                this.openNextAfterReview(submission.id);
            });
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
                this.isLeader = viewerRole?.viewerRole === 'Leader';
                const rejectMessage = this.resolveRejectMessage();
                if (rejectMessage) {
                    this.messageInfo(rejectMessage);
                    this.navigateToList();
                    return;
                }
                this.watchSubmission();
            });
    }

    // Có kết quả đang chấm dở thì hỏi trước khi rời màn (44.13)
    canDeactivate(): boolean | Promise<boolean> {
        return this._allowLeave || !this.isDirty ? true : this.confirmLeave();
    }

    // Ctrl + Enter gửi, Ctrl + ↑ / ↓ sang văn bản trước / sau, bắt cả khi đang gõ lý do; phím trong dialog (hỏi, Lịch sử) thì bỏ qua
    onKeydown(event: KeyboardEvent) {
        if (!event.ctrlKey || (event.target as HTMLElement).closest('.p-dialog')) return;
        if (event.key === 'Enter') {
            event.preventDefault();
            this.submitReview();
        } else if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
            event.preventDefault();
            this.navigate(event.key === 'ArrowUp' ? 'previous' : 'next');
        }
    }

    onBeforeUnload(event: BeforeUnloadEvent) {
        if (this.isDirty) {
            event.preventDefault();
        }
    }

    // ◀ ▶ / "Để sau" đi trong bộ lọc (44.11); đang chấm dở thì hỏi trước khi gọi BE
    async navigate(direction: AdjacentDirection) {
        if (!this.submission || this.isBusy || this.isEdge(direction)) return;
        if (this.isDirty && !(await this.confirmLeave())) return;
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

    onResizeEnd(event: SplitterResizeEndEvent) {
        this.resizing = false;
        this.saveLayout({ ...this.layout, sizes: [Number(event.sizes[0]), Number(event.sizes[1])] });
    }

    // Dự án không bật QA / người xem là Nhập liệu thì không vào màn, không gọi GET {id}/qa (44.3)
    private resolveRejectMessage() {
        if (!this.project!.isQaEnabled) return 'Dự án không bật QA';
        if (this.role === SUBMISSION_ROLE.DATA) return 'Màn QA không dành cho Nhập liệu';
        return null;
    }

    // :submissionId đổi khi ◀ ▶ mà component không tạo lại; tải song song chi tiết + lượt QA, switchMap bỏ kết quả cũ còn đang tải
    private watchSubmission() {
        combineLatest([this._activatedRoute.paramMap, this._activatedRoute.queryParamMap, this._reload$.pipe(startWith(undefined))])
            .pipe(
                switchMap(([params, queryParams]) => {
                    this.query = readSubmissionQuery(queryParams);
                    const submissionId = Number(params.get('submissionId'));
                    this.loading = true;
                    return forkJoin([this._submissionService.findQaById(submissionId, this.detailQuery()), this.loadQaLogs(submissionId)]).pipe(
                        finalize(() => (this.loading = false)),
                    );
                }),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe(([res, qaLogs]) => {
                if (!this.isResponseSucceed(res, false)) return this.handleSubmissionError(res);
                this.isNotAssigned = false;
                this.applySubmission(res.data, qaLogs);
            });
    }

    // Đổi sang văn bản khác thì bỏ đánh dấu (44.13); tải lại cùng văn bản thì giữ để không mất kết quả đang chấm
    private applySubmission(submission: IQaSubmissionDetail, qaLogs: ISubmissionQaLog[] | null) {
        if (submission.id !== this.submission?.id) {
            this.clearMarks();
            this.noMore = { previous: false, next: false };
        }
        this._allowLeave = false;
        this.submission = submission;
        this.qaLogs = qaLogs;
        this.readonlyBanner = this.resolveReadonlyBanner(submission);
        const fieldIds = new Set(submission.fields.map((field) => field.documentFieldId));
        this.returnedFieldIds =
            submission.statusCheck === SUBMISSION_CHECK_STATUS.RETURNED ? new Set(submission.qaWrongDocumentFieldIds) : new Set();
        this.previousWrongLog = this.isWaitingQa ? (qaLogs?.find((log) => log.newStatusQa === SUBMISSION_QA_STATUS.FAILED) ?? null) : null;
        this.previousWrongFieldIds = new Set(this.previousWrongLog?.wrongDocumentFieldIds.filter((id) => fieldIds.has(id)));
        this.visibleFields = submission.fields.filter((field) => field.isShowListView || this.previousWrongFieldIds.has(field.documentFieldId));
    }

    // Xét theo thứ tự, lấy điều kiện đầu tiên khớp (44.9); Leader xét trước thiếu quyền vì Leader không có quyền chấm QA
    private resolveReadonlyBanner(submission: IQaSubmissionDetail): IQaBanner | null {
        if (this.isLeader) return { tone: 'gray', message: 'Leader chỉ xem kết quả QA, không chấm.' };
        if (!this._sharedService.isGranted(PermissionConstants.FUNCTION_SUBMISSION_QA)) {
            return { tone: 'gray', message: 'Bạn chỉ có quyền xem văn bản.' };
        }
        if (!this._sharedService.isSuperAdmin()) {
            if (this.project!.status === PROJECT_STATUS.DONE) return { tone: 'gray', message: 'Dự án đã xong — chỉ xem.' };
            if (this.project!.status === PROJECT_STATUS.EXPIRED) return { tone: 'gray', message: 'Dự án đã hết hạn — chỉ xem.' };
        }
        if (submission.statusCheck === SUBMISSION_CHECK_STATUS.NOT_CHECKED) {
            return { tone: 'gray', message: 'Văn bản chưa được kiểm — chưa chấm QA được.', isFinished: true };
        }
        const qaText = `${this.formatBannerDate(submission.qaDate)} bởi ${submission.qaUserName || '—'}`;
        if (submission.statusCheck === SUBMISSION_CHECK_STATUS.RETURNED) {
            return {
                tone: 'red',
                title: `Đã trả lại lúc ${qaText} · đang chờ người nhập sửa.`,
                message: `Văn bản tự quay lại Chưa QA khi người nhập kiểm xong. Lý do đã gửi: “${submission.qaReason || '—'}”`,
                isFinished: true,
            };
        }
        if (submission.statusQa === SUBMISSION_QA_STATUS.PASSED) {
            return {
                tone: 'green',
                title: `Đã đạt lúc ${qaText}.`,
                message: 'Văn bản đã khoá — người nhập không sửa được nữa.',
                isFinished: true,
            };
        }
        return null;
    }

    private formatBannerDate(date: string | null) {
        return date ? formatDate(date, BANNER_DATE_FORMAT, this._locale) : '—';
    }

    // sh-input-text không lộ ô nhập: tìm theo id rồi focus
    private focusReason() {
        setTimeout(() => (this._element.querySelector(`#${REASON_INPUT_ID}`) as HTMLTextAreaElement | null)?.focus());
    }

    // Chấm xong sang văn bản kế trong bộ lọc; hết văn bản thì ở lại, tải lại để thấy kết quả vừa chấm và tắt ▶
    private openNextAfterReview(submissionId: number) {
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

    // Giống confirmAction nhưng trả Promise có cả nhánh ở lại (guard / ◀ ▶ cần biết người dùng chọn gì)
    private confirmLeave(): Promise<boolean> {
        return new Promise((resolve) =>
            this._confirmationService.confirm({
                header: 'Bỏ kết quả đang chấm?',
                message: 'Các trường đã đánh dấu Sai và lý do sẽ mất.',
                closable: true,
                closeOnEscape: true,
                rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
                acceptButtonProps: { label: 'Bỏ và rời đi', severity: 'danger' },
                accept: () => resolve(true),
                reject: () => resolve(false),
            }),
        );
    }

    // Đổi văn bản thay cho mục lịch sử hiện tại, giữ bộ lọc + tên metadata; đã hỏi / đã gửi nên guard không hỏi lại
    private openSubmission(submissionId: number) {
        this._allowLeave = true;
        this.router.navigate([PROJECT_URL, this._projectId, 'metadata', this._documentTypeId, 'qa', submissionId], {
            queryParams: { ...this.query, [DOCUMENT_TYPE_NAME_PARAM]: this._documentTypeName || null },
            replaceUrl: true,
        });
    }

    // Mọi lỗi: toast câu BE, giữ đánh dấu + lý do; văn bản / phiếu / dự án đổi trạng thái → tải lại; 820 → trạng thái trống;
    // 305 / 319 → về danh sách (44.18)
    private handleReviewError(res: IBaseResponse) {
        if (
            res.code === SUBMISSION_ERROR_CODE.NOT_ASSIGNED ||
            res.code === SUBMISSION_ERROR_CODE.SUBMISSION_NOT_FOUND ||
            res.code === SUBMISSION_ERROR_CODE.NOT_IN_DOCUMENT_TYPE
        ) {
            this.handleSubmissionError(res);
            return;
        }
        this.messageError(res.message);
        if (RELOAD_AFTER_REVIEW_CODES.includes(res.code)) {
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

    // Lỗi riêng lượt QA (kể cả lỗi HTTP) → null, không làm hỏng lần tải chi tiết
    private loadQaLogs(submissionId: number) {
        return this._submissionService.qaLogs(submissionId).pipe(
            map((res) => (this.isResponseSucceed(res, false) ? res.data.items : null)),
            catchError(() => of(null)),
        );
    }

    private messageInfo(detail: string) {
        this._messageService.add({ closable: true, severity: 'info', detail, life: 4000 });
    }

    private detailQuery(): IFindSubmissionDetail {
        return { ...this.buildFilter(), includePosition: true };
    }

    // Bộ lọc mang theo từ màn danh sách (44.2); view chỉ để quay về đúng chế độ, không gửi BE
    private buildFilter(): ISubmissionFilter {
        const { pageNumber, pageSize, view, ...filter } = this.query;
        return { ...filter, projectId: this._projectId, documentTypeId: this._documentTypeId };
    }

    private navigateToList() {
        this.router.navigate([PROJECT_URL, this._projectId, 'metadata', this._documentTypeId], { queryParams: this.query });
    }

    // 820 → trạng thái trống; 305 / 319 → về màn danh sách; còn lại xử lý như lỗi dự án (44.17)
    private handleSubmissionError(res: IBaseResponse) {
        if (res.code === SUBMISSION_ERROR_CODE.NOT_ASSIGNED) {
            this.submission = undefined;
            this.isNotAssigned = true;
            return;
        }
        if (res.code === SUBMISSION_ERROR_CODE.SUBMISSION_NOT_FOUND || res.code === SUBMISSION_ERROR_CODE.NOT_IN_DOCUMENT_TYPE) {
            this.messageError(res.message);
            // Văn bản không còn chấm được: rời màn không hỏi bỏ kết quả
            this._allowLeave = true;
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
