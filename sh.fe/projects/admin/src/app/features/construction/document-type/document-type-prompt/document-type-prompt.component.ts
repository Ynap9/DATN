import { Component, DestroyRef, inject, input, OnInit, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { ShInputTextComponent, ShSelectComponent } from '@sh/ui';
import { CheckboxModule } from 'primeng/checkbox';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { EMPTY, finalize, map, of, switchMap } from 'rxjs';
import { IPromptTemplateInfo, ISavePromptDraft, IViewPromptEditor } from '@/models/prompt-template.model';
import { PromptTemplateService } from '@/services/prompt-template.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { TblAction } from '@/shared/components/tbl-action/tbl-action.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import {
    getPromptTemplateStatusBadge,
    NORMALIZE_RULES,
    PROMPT_FIELD_DESCRIPTION_MAX_LENGTH,
    PROMPT_ERROR_CODE,
    PROMPT_FIELD_EXAMPLE_MAX_LENGTH,
    PROMPT_MAX_LENGTH,
    PROMPT_NOTE_MAX_LENGTH,
} from '@/shared/constants/prompt-template.constants';
import { ITableAction, ITableBadge } from '@/shared/models/data-table.models';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { SharedImports } from '@/shared/import.shared';
import { IPromptHistoryDialogData, PromptHistoryDialogComponent } from './prompt-history-dialog.component';
import { IPromptPreviewDialogData, PromptPreviewDialogComponent } from './prompt-preview-dialog.component';
import { IPromptPublishDialogData, PromptPublishDialogComponent } from './prompt-publish-dialog.component';

// sh-input-text phát undefined khi xoá trắng ô vốn có chữ (rơi về firstValue), nên mọi ô chữ đọc qua hàm này
function toText(value: string | null | undefined): string {
    return value?.trim() ?? '';
}

type PromptFieldForm = FormGroup<{
    documentFieldId: FormControl<number>;
    description: FormControl<string>;
    example: FormControl<string>;
    normalizeRule: FormControl<string>;
    isRequired: FormControl<boolean>;
    isActive: FormControl<boolean>;
}>;

@Component({
    selector: 'app-document-type-prompt',
    imports: [SharedImports, CheckboxModule, ToggleSwitchModule, ShInputTextComponent, ShSelectComponent, StatusBadge, TblAction],
    templateUrl: './document-type-prompt.component.html',
    styleUrl: './document-type-prompt.component.scss',
})
export class DocumentTypePromptComponent extends BaseComponent implements OnInit {
    private readonly _promptTemplateService = inject(PromptTemplateService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly documentTypeId = input.required<number>();
    readonly openFieldsTab = output<void>();

    readonly canUpdate = this._sharedService.isGranted(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_UPDATE);
    readonly canPublish = this._sharedService.isGranted(PermissionConstants.FUNCTION_AI_PROMPT_TEMPLATE_PUBLISH);
    readonly draftActions: ITableAction[] = [{ type: 'discard', label: 'Bỏ nháp', icon: 'pi pi-trash', severity: 'danger' }];
    readonly normalizeRules = NORMALIZE_RULES;
    readonly promptMaxLength = PROMPT_MAX_LENGTH;
    readonly noteMaxLength = PROMPT_NOTE_MAX_LENGTH;
    readonly descriptionMaxLength = PROMPT_FIELD_DESCRIPTION_MAX_LENGTH;
    readonly exampleMaxLength = PROMPT_FIELD_EXAMPLE_MAX_LENGTH;

    override form = new FormGroup({
        systemPrompt: new FormControl('', { nonNullable: true }),
        instructionPrompt: new FormControl('', { nonNullable: true }),
        note: new FormControl('', { nonNullable: true }),
        fields: new FormArray<PromptFieldForm>([]),
    });

    editor?: IViewPromptEditor;
    submitting = false;
    publishing = false;

    // Bản đang hiện trên form: nháp nếu có, không thì bản đang dùng
    get editing(): IPromptTemplateInfo | null | undefined {
        return this.editor?.draft ?? this.editor?.published;
    }

    get editingBadge(): ITableBadge | null {
        return this.editing ? getPromptTemplateStatusBadge(this.editing.status) : null;
    }

    // Màn chi tiết hỏi trước khi đổi tab / rời trang
    get isDirty(): boolean {
        return this.form.dirty;
    }

    // Không có nháp và form chưa sửa thì không có gì để xuất bản
    get hasPublishable(): boolean {
        return !!this.editor?.draft || this.form.dirty;
    }

    countLength(value: string | null | undefined): number {
        return toText(value).length;
    }

    override ngOnInit(): void {
        this.loadEditor();
    }

    loadEditor(onLoaded?: (editor: IViewPromptEditor) => void) {
        this.loading = true;
        this._promptTemplateService
            .findEditor(this.documentTypeId())
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.editor = res.data;
                    this.fillForm(res.data);
                    onLoaded?.(res.data);
                }
            });
    }

    saveDraft() {
        this.submitting = true;
        this.saveDraftRequest()
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.loadEditor((editor) => this.messageSuccess(`Đã lưu nháp v${editor.draft?.version}`));
                } else {
                    this.reloadIfDraftChanged(res);
                }
            });
    }

    openPublishDialog() {
        const data: IPromptPublishDialogData = {
            version: this.editor?.draft?.version,
            publishedVersion: this.editor?.published?.version,
            note: toText(this.form.controls.note.value),
        };
        this._dialogService
            .open(PromptPublishDialogComponent, { header: 'Xuất bản prompt', width: '32rem', modal: true, closable: true, data })
            ?.onClose.subscribe((note?: string | null) => note != null && this.publish(note));
    }

    openHistoryDialog() {
        const data: IPromptHistoryDialogData = { documentTypeId: this.documentTypeId(), canRestore: this.canUpdate, isDirty: this.isDirty };
        this._dialogService
            .open(PromptHistoryDialogComponent, { header: 'Lịch sử phiên bản', width: '56rem', modal: true, closable: true, data })
            ?.onClose.subscribe((restored) => restored && this.loadEditor());
    }

    openPreviewDialog() {
        if (!this.editor?.editingId) return;

        const data: IPromptPreviewDialogData = { templateId: this.editor.editingId, isDirty: this.isDirty };
        this._dialogService.open(PromptPreviewDialogComponent, { header: 'Xem prompt', width: '48rem', modal: true, closable: true, data });
    }

    confirmDiscard() {
        const draft = this.editor?.draft;
        if (!draft) return;

        this.confirmDelete({ header: 'Bỏ nháp', message: `Bỏ bản nháp v${draft.version}? Nội dung nháp sẽ mất.` }, () => {
            this._promptTemplateService
                .discard(draft.id)
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe((res) => {
                    if (this.isResponseSucceed(res, true, `Đã bỏ nháp v${draft.version}`)) {
                        this.loadEditor();
                    }
                });
        });
    }

    // Form đang sửa hoặc chưa có nháp thì lưu nháp trước để xuất bản đúng nội dung đang thấy (một lần bấm)
    private publish(note: string) {
        const draft = this.editor?.draft;
        // BE xuất bản giữ ghi chú cũ khi note rỗng, nên ghi chú sửa / xoá trong dialog phải đi qua lưu nháp
        if (this.canUpdate && toText(note) !== toText(this.form.controls.note.value)) {
            this.form.controls.note.setValue(toText(note));
            this.form.controls.note.markAsDirty();
        }
        const needsSave = !draft || this.form.dirty;
        const draftId$ = needsSave
            ? this.saveDraftRequest().pipe(
                  map((res) => {
                      const isSucceed = this.isResponseSucceed(res);
                      if (!isSucceed) this.reloadIfDraftChanged(res);
                      return isSucceed ? res.data : null;
                  }),
              )
            : of(draft.id);

        this.publishing = true;
        draftId$
            .pipe(
                // Vừa lưu nháp thì không cần so ngày; chỉ xuất bản nháp đang xem thì gửi ngày sửa cuối để BE chặn khi bị người khác sửa (914)
                switchMap((draftId) =>
                    draftId
                        ? this._promptTemplateService.publish(draftId, { note: toText(note) || null, draftModifiedDate: needsSave ? null : (draft?.modifiedDate ?? null) })
                        : EMPTY,
                ),
                finalize(() => (this.publishing = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.loadEditor((editor) => this.messageSuccess(`Đã xuất bản v${editor.published?.version}`));
                } else if (needsSave) {
                    // Nháp đã lưu nhưng xuất bản bị chặn (vd. 911 / 913): tải lại để form khớp bản nháp vừa lưu
                    this.loadEditor();
                } else {
                    this.reloadIfDraftChanged(res);
                }
            });
    }

    // 914: nháp đã bị người khác sửa. BE trả câu báo (đã toast), tải lại để thấy bản mới nhất, nội dung đang gõ mất
    private reloadIfDraftChanged(res: IBaseResponse) {
        if (res.code === PROMPT_ERROR_CODE.DRAFT_CHANGED) {
            this.loadEditor();
        }
    }

    private saveDraftRequest() {
        return this._promptTemplateService.saveDraft(this.documentTypeId(), this.buildDraftBody());
    }

    // Dựng lại form theo bản đang xem; mọi trường OCR hiện có đều lên lưới (trường chưa có trong bản nhận giá trị mặc định từ BE)
    private fillForm(editor: IViewPromptEditor) {
        this.form.controls.fields.clear();
        editor.fields.forEach((field) =>
            this.form.controls.fields.push(
                new FormGroup({
                    documentFieldId: new FormControl(field.documentFieldId, { nonNullable: true }),
                    description: new FormControl(field.description, { nonNullable: true }),
                    example: new FormControl(field.example ?? '', { nonNullable: true }),
                    normalizeRule: new FormControl(field.normalizeRule, { nonNullable: true }),
                    isRequired: new FormControl(field.isRequired, { nonNullable: true }),
                    isActive: new FormControl(field.isActive, { nonNullable: true }),
                }),
            ),
        );
        // Ghi chú thuộc từng phiên bản: chỉ điền của nháp, đang xem bản đang dùng thì để trống cho nháp sắp tạo
        this.form.patchValue({ systemPrompt: editor.systemPrompt, instructionPrompt: editor.instructionPrompt, note: editor.draft?.note ?? '' });
        if (!this.canUpdate) {
            this.form.disable();
        }
        this.form.markAsPristine();
    }

    // Gửi toàn bộ trường đang hiện: trường đã xoá / tắt OCR không còn trên lưới nên tự rơi khỏi nháp
    private buildDraftBody(): ISavePromptDraft {
        const value = this.form.getRawValue();
        return {
            systemPrompt: toText(value.systemPrompt),
            instructionPrompt: toText(value.instructionPrompt),
            note: toText(value.note) || null,
            draftModifiedDate: this.editor?.draft?.modifiedDate ?? null,
            fields: value.fields.map((field) => ({ ...field, description: toText(field.description), example: toText(field.example) || null })),
        };
    }
}
