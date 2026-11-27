import { Component, DestroyRef, effect, inject, input, output, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription, switchMap, takeWhile, timer } from 'rxjs';
import { ISheetHeader } from '@/models/document-field.model';
import { IViewProjectPair } from '@/models/project.model';
import {
    IImportExcelOverwriteSubmission,
    IImportExcelSubmission,
    IImportOverwriteSubmission,
    IImportSheetSubmission,
    IImportSubmission,
    IUploadImportExcelResult,
} from '@/models/submission.model';
import { ProjectService } from '@/services/project.service';
import { SubmissionService } from '@/services/submission.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PROJECT_URL } from '@/shared/constants/project.constants';
import {
    BACKGROUND_JOB_STATUS,
    IMPORT_KIND,
    IMPORT_SOURCE,
    ImportKind,
    ImportSource,
    JOB_POLL_MS,
    SUBMISSION_ERROR_CODE,
} from '@/shared/constants/submission.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import {
    createColumnMapping,
    getVerifyView,
    IImportColumnMapping,
    IImportFieldRow,
    IMPORT_DEFAULT_START_COLUMN,
    IMPORT_FIXED_COLUMN_KEYS,
    IMPORT_JOB_RUNNING_MESSAGE,
    IMPORT_KIND_OPTIONS,
    IMPORT_STEPS,
    isColumnMappingComplete,
    IVerifyImportResult,
    TOAST_ERROR_CODES,
    toHeaderKey,
} from '../data-transfer.constants';
import { ImportFieldStepComponent } from '../import-field-step/import-field-step.component';
import { ImportKindStepComponent } from '../import-kind-step/import-kind-step.component';
import { ImportSourceStepComponent } from '../import-source-step/import-source-step.component';
import { ImportVerifyStepComponent } from '../import-verify-step/import-verify-step.component';

const STEP = {
    SOURCE: 0,
    FIELD: 1,
    KIND: 2,
    VERIFY: 3,
} as const;

// Wizard import 4 bước của tab Nhập (48.5); đổi cặp → về bước 1, bỏ file đã tải (48.4)
@Component({
    selector: 'app-import-wizard',
    imports: [SharedImports, ImportSourceStepComponent, ImportFieldStepComponent, ImportKindStepComponent, ImportVerifyStepComponent],
    templateUrl: './import-wizard.component.html',
    host: { '(window:beforeunload)': 'onBeforeUnload($event)' },
})
export class ImportWizardComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _submissionService = inject(SubmissionService);
    private readonly _destroyRef = inject(DestroyRef);

    pair = input.required<IViewProjectPair>();
    // true khi đang import: màn cha khoá tab (48.4)
    importingChange = output<boolean>();
    // Import xong: màn cha tải lại số văn bản (48.14)
    imported = output<void>();
    // 337: không còn quản lý dự án, màn cha tải lại danh sách cặp (48.18)
    forbidden = output<void>();
    private readonly _sourceStep = viewChild(ImportSourceStepComponent);
    private readonly _fieldStep = viewChild(ImportFieldStepComponent);

    readonly stepKey = STEP;
    readonly steps = IMPORT_STEPS;

    step: number = STEP.SOURCE;
    source: ImportSource = IMPORT_SOURCE.SHEET;
    kind: ImportKind = IMPORT_KIND.APPEND;
    upload: IUploadImportExcelResult | null = null;
    sheetName: string | null = null;
    // Bước 1 nguồn Google: link người dùng dán, danh sách sheet đã lấy, sheet đã chọn; cột bắt đầu đọc dùng cho cả Google và Excel
    url = '';
    googleSheets: ISheetHeader[] = [];
    googleSheet: string | null = null;
    startColumn = IMPORT_DEFAULT_START_COLUMN;
    // Bước 2 ghép trường: null = chưa tải từ BE; xoá khi bước 1 đổi để bước 2 gọi match lại (63.10)
    fieldRows: IImportFieldRow[] | null = null;
    fieldError: string | null = null;
    isSavingFields = false;
    private _sourceSignature = '';
    mapping: IImportColumnMapping = createColumnMapping();
    // Tên sheet Google gắn với dự án chỉ có ở chi tiết dự án, không có trong cặp
    googleSheetName: string | null = null;

    verifyResult: IVerifyImportResult | null = null;
    // Lỗi chạy thử hiện dưới lưới cột ở bước 3, lỗi import hiện ở bước 4
    verifyError: string | null = null;
    importError: string | null = null;
    isVerifying = false;
    isImporting = false;
    // Khác null = import xong, thay nội dung wizard bằng khối Xong
    doneMessage: string | null = null;
    private _requestSubscription: Subscription | null = null;

    get verifyView() {
        return this.verifyResult ? getVerifyView(this.kind, this.verifyResult, !!this.appendPageCountColumn) : null;
    }

    get isBusy() {
        return this.isVerifying || this.isImporting || this.isSavingFields;
    }

    // Chỉ import Google chạy đồng bộ: rời trang là không biết kết quả
    get isLeaveBlocked() {
        return this.isImporting && this.source === IMPORT_SOURCE.SHEET;
    }

    get runningMessage() {
        return this.isImporting ? IMPORT_JOB_RUNNING_MESSAGE[this.source] : null;
    }

    // Nguồn · kiểu · các cột đã chọn
    // Cột Số trang chỉ thuộc kiểu Thêm mới; kiểu khác gửi '' (48.15)
    private get appendPageCountColumn() {
        return this.kind === IMPORT_KIND.APPEND ? this.mapping.pageCountColumn.trim() : '';
    }

    // Nguồn · kiểu · số trường đã ghép · các cột cố định đã chọn (63.8)
    get summaryChips() {
        const columns = [this.mapping.fileColumnImport, this.mapping.profileRange, this.mapping.box, this.appendPageCountColumn].map((column) => column.trim()).filter(Boolean);
        const sourceChip = this.source === IMPORT_SOURCE.SHEET ? `Google Sheet · ${this.googleSheet}` : `Excel · ${this.upload?.fileName} · ${this.sheetName}`;
        return [sourceChip, this.getStepSummary(STEP.KIND), `Đã ghép ${this.fieldRows?.length ?? 0} trường`, columns.join(' · ')];
    }

    get primaryLabel() {
        if (this.step === STEP.FIELD && this.isSavingFields) return 'Đang lưu…';
        if (this.step === STEP.KIND) return this.isVerifying ? 'Đang chạy thử…' : 'Chạy thử';
        if (this.step !== STEP.VERIFY) return 'Tiếp tục';
        if (this.importError) return this.isVerifying ? 'Đang chạy thử…' : 'Thử lại';
        return this.verifyView?.actionLabel ?? '';
    }

    get canPrimary() {
        if (this.isBusy) return false;
        switch (this.step) {
            case STEP.SOURCE:
                return this.source === IMPORT_SOURCE.SHEET ? !!this.googleSheet : !!this.upload && !!this.sheetName;
            case STEP.FIELD:
                return !!this._fieldStep() && !this._fieldStep()!.isBlocked;
            case STEP.KIND:
                return isColumnMappingComplete(this.mapping, this.kind);
            default:
                return !!this.importError || !!this.verifyView?.main.value;
        }
    }

    // Header của sheet đang chọn ở bước 1, làm gợi ý ở bước Ghép cột
    get sheetHeaders() {
        const isGoogle = this.source === IMPORT_SOURCE.SHEET;
        const sheets = isGoogle ? this.googleSheets : (this.upload?.sheets ?? []);
        return sheets.find((sheet) => sheet.sheetName === (isGoogle ? this.googleSheet : this.sheetName))?.headers ?? [];
    }

    constructor() {
        super();
        effect(() => {
            const pair = this.pair();
            untracked(() => this.reset(pair));
        });
    }

    // Dòng xám dưới tên bước: lựa chọn đã chọn, chỉ hiện từ bước đang đứng trở về trước
    getStepSummary(index: number) {
        if (index > this.step) return '';
        if (index === STEP.SOURCE) {
            const sheetName = this.source === IMPORT_SOURCE.SHEET ? this.googleSheet : this.sheetName;
            if (!sheetName) return '';
            return this.source === IMPORT_SOURCE.SHEET ? `${sheetName} · Google Sheet` : `${this.upload?.fileName} · ${sheetName}`;
        }
        if (index === STEP.FIELD) return this.fieldRows ? `Đã ghép ${this.fieldRows.filter((row) => row.columnIndex !== null).length} / ${this.fieldRows.length} trường` : '';
        if (index === STEP.KIND) return IMPORT_KIND_OPTIONS.find((option) => option.value === this.kind)?.label ?? '';
        return '';
    }

    onPrimary() {
        if (!this.canPrimary) return;
        if (this.step === STEP.KIND || (this.step === STEP.VERIFY && this.importError)) {
            this.verify();
        } else if (this.step === STEP.VERIFY) {
            this.runImport();
        } else if (this.step === STEP.FIELD) {
            if (this._fieldStep()!.validateRequired()) this.saveFieldMapping();
        } else {
            if (this.step === STEP.SOURCE) this.resetWhenSourceChanged();
            this.step++;
        }
    }

    // Rời bước 4 là bỏ kết quả chạy thử: sang lại bước 4 luôn phải chạy thử (48.12)
    back() {
        if (this.step === STEP.VERIFY) this.clearVerify();
        this.step--;
    }

    // Về bước 1, giữ link + sheet / file Excel đã tải; ghép đọc lại từ BE khi sang bước 2 (63.11)
    importMore() {
        this.doneMessage = null;
        this.clearVerify();
        this.fieldRows = null;
        this.fieldError = null;
        this.step = STEP.SOURCE;
    }

    // 211 / 337 khi tải ghép: toast (337 kèm tải lại danh sách cặp) (63.13)
    onFieldToastFailed(res: IBaseResponse) {
        this.toBoxError(res);
    }

    openSubmissionList() {
        this.router.navigate([PROJECT_URL, this.pair().projectId, 'metadata', this.pair().documentTypeId]);
    }

    onBeforeUnload(event: BeforeUnloadEvent) {
        if (this.isLeaveBlocked) event.preventDefault();
    }

    // Bước 1 đổi link / sheet / file / cột bắt đầu thì ghép cũ không còn đúng: bước 2 tải lại từ BE, ô cột cố định không còn trong header mới bị xoá (63.10)
    private resetWhenSourceChanged() {
        const signature = [this.source, this.url.trim(), this.googleSheet, this.upload?.fileKey, this.sheetName, this.startColumn].join('|');
        if (signature === this._sourceSignature) return;
        this._sourceSignature = signature;
        this.fieldRows = null;
        this.fieldError = null;
        this.clearMissingFixedColumns();
    }

    private clearMissingFixedColumns() {
        const headerKeys = new Set(this.sheetHeaders.slice(this.startColumn - 1).map(toHeaderKey));
        const mapping = { ...this.mapping };
        for (const key of IMPORT_FIXED_COLUMN_KEYS) {
            if (mapping[key] && !headerKeys.has(toHeaderKey(mapping[key]))) mapping[key] = '';
        }
        this.mapping = mapping;
    }

    // Luôn lưu đủ mọi trường (63.5); BE bỏ qua khi không đổi. Lỗi → khung đỏ dưới bảng, ở lại bước
    private saveFieldMapping() {
        const headers = this.sheetHeaders;
        this.isSavingFields = true;
        this.fieldError = null;
        this._requestSubscription = this._submissionService
            .saveImportFieldMapping({
                projectId: this.pair().projectId,
                documentTypeId: this.pair().documentTypeId,
                headers,
                startColumn: this.startColumn,
                fields: this.fieldRows!.map((row) => ({ documentFieldId: row.documentFieldId, fieldImport: headers[row.columnIndex!], columnIndex: row.columnIndex! })),
            })
            .pipe(
                finalize(() => (this.isSavingFields = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.step++;
                    return;
                }
                this.fieldError = this.toBoxError(res);
            });
    }

    // Lỗi chạy thử (kể cả bấm Thử lại ở bước 4) → về bước 3, khung đỏ câu BE trả (48.11)
    private verify() {
        const request =
            this.source === IMPORT_SOURCE.SHEET
                ? this._submissionService.verifyImport(this.kind, this.buildSheetBody())
                : this._submissionService.verifyImportExcel(this.kind, this.buildExcelBody());
        this.isVerifying = true;
        this._requestSubscription = request
            .pipe(
                finalize(() => (this.isVerifying = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, false)) {
                    this.clearVerify();
                    this.verifyResult = res.data;
                    this.step = STEP.VERIFY;
                    return;
                }
                const error = this.toBoxError(res);
                if (!error) return;
                this.clearVerify();
                this.verifyError = error;
                this.step = STEP.KIND;
            });
    }

    // Google import đồng bộ; Excel tạo job rồi hỏi trạng thái (48.13)
    private runImport() {
        this.setImporting(true);
        if (this.source === IMPORT_SOURCE.SHEET) {
            this._requestSubscription = this._submissionService
                .importSheet(this.kind, this.buildSheetBody())
                .pipe(
                    finalize(() => this.setImporting(false)),
                    takeUntilDestroyed(this._destroyRef),
                )
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.finish(res.data.message);
                    } else {
                        this.importError = this.toBoxError(res);
                    }
                });
            return;
        }

        this._requestSubscription = this._submissionService
            .createImportExcelJob(this.kind, this.buildExcelBody())
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.pollJob(res.data.jobId);
                    } else {
                        this.importError = this.toBoxError(res);
                        this.setImporting(false);
                    }
                },
                error: () => this.setImporting(false),
            });
    }

    // Hỏi job mỗi JOB_POLL_MS tới khi xong / lỗi; 344 (job không thấy) cũng dừng
    private pollJob(jobId: string) {
        this._requestSubscription = timer(0, JOB_POLL_MS)
            .pipe(
                switchMap(() => this._submissionService.findImportJob(jobId)),
                takeWhile((res) => this.isResponseSucceed(res, false) && res.data.status < BACKGROUND_JOB_STATUS.DONE, true),
                finalize(() => this.setImporting(false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false)) {
                    this.importError = this.toBoxError(res);
                } else if (res.data.status === BACKGROUND_JOB_STATUS.DONE) {
                    this.finish(res.data.resultMessage ?? '');
                } else if (res.data.status === BACKGROUND_JOB_STATUS.FAILED) {
                    this.importError = res.data.errorMessage;
                }
            });
    }

    // 211 / 337 báo bằng toast (337 kèm tải lại danh sách cặp) và trả null; lỗi khác trả câu BE để hiện khung đỏ (48.18)
    private toBoxError(res: IBaseResponse) {
        if (!TOAST_ERROR_CODES.includes(res.code)) return res.message;
        this.messageError(res.message);
        if (res.code === SUBMISSION_ERROR_CODE.PROJECT_NOT_MANAGED) this.forbidden.emit();
        return null;
    }

    private finish(message: string) {
        this.doneMessage = message;
        this.imported.emit();
    }

    private setImporting(value: boolean) {
        if (this.isImporting === value) return;
        this.isImporting = value;
        this.importingChange.emit(value);
    }

    private clearVerify() {
        this.verifyResult = null;
        this.verifyError = null;
        this.importError = null;
    }

    // Body chung (48.15): ô trống gửi ''; ghi đè không dùng cột link
    private buildBaseBody(): IImportSubmission {
        return {
            projectId: this.pair().projectId,
            documentTypeId: this.pair().documentTypeId,
            fileColumnImport: this.mapping.fileColumnImport.trim(),
            linkColumnImport: this.kind === IMPORT_KIND.OVERWRITE ? '' : this.mapping.linkColumnImport.trim(),
            profileRange: this.mapping.profileRange.trim(),
            box: this.mapping.box.trim(),
            pageCountColumn: this.appendPageCountColumn,
            startColumn: this.startColumn,
        };
    }

    private buildOverwriteFields() {
        return {
            columnImport: this.mapping.columnImport.trim(),
            documentFieldId: this.mapping.documentFieldId!,
            overwriteChecked: this.mapping.overwriteChecked,
        };
    }

    // Cả 3 kiểu đọc link + sheet người dùng chọn ở bước 1 (63.9)
    private buildSheetBody(): IImportSheetSubmission | IImportOverwriteSubmission {
        const body: IImportSheetSubmission = { ...this.buildBaseBody(), url: this.url.trim(), sheetName: this.googleSheet! };
        return this.kind === IMPORT_KIND.OVERWRITE ? { ...body, ...this.buildOverwriteFields() } : body;
    }

    // Excel ghi đè không có url
    private buildExcelBody(): IImportExcelSubmission | IImportExcelOverwriteSubmission {
        const body: IImportExcelSubmission = { ...this.buildBaseBody(), fileKey: this.upload!.fileKey, sheetName: this.sheetName! };
        return this.kind === IMPORT_KIND.OVERWRITE ? { ...body, ...this.buildOverwriteFields() } : body;
    }

    // Mặc định nguồn Google, link điền sẵn theo dự án; bỏ lượt tải file / chạy thử / import đang chờ của cặp cũ
    private reset(pair: IViewProjectPair) {
        this._sourceStep()?.changeFile();
        this._requestSubscription?.unsubscribe();
        this.isVerifying = false;
        this.setImporting(false);
        this.clearVerify();
        this.doneMessage = null;
        this.step = STEP.SOURCE;
        this.source = IMPORT_SOURCE.SHEET;
        this.kind = IMPORT_KIND.APPEND;
        this.upload = null;
        this.sheetName = null;
        this.url = pair.urlGoogleSheetSync ?? '';
        this.googleSheets = [];
        this.googleSheet = null;
        this.startColumn = IMPORT_DEFAULT_START_COLUMN;
        this.fieldRows = null;
        this.fieldError = null;
        this.isSavingFields = false;
        this._sourceSignature = '';
        this.mapping = createColumnMapping();
        this.googleSheetName = null;
        if (pair.urlGoogleSheetSync) this.loadGoogleSheetName(pair);
    }

    // Đổi cặp nhanh thì bỏ kết quả của cặp cũ về sau
    private loadGoogleSheetName(pair: IViewProjectPair) {
        this._projectService
            .findById(pair.projectId)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (pair !== this.pair() || !this.isResponseSucceed(res, false)) return;
                this.googleSheetName = res.data.googleSheetName;
            });
    }
}
