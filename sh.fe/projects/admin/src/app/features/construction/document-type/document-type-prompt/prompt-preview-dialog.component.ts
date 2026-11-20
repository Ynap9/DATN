import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DynamicDialogConfig } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { PromptTemplateService } from '@/services/prompt-template.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

export interface IPromptPreviewDialogData {
    templateId: number;
    isDirty: boolean;
}

// Xem prompt ghép từ bản đã lưu (không gồm thay đổi đang sửa trên form)
@Component({
    selector: 'app-prompt-preview-dialog',
    imports: [SharedImports],
    templateUrl: './prompt-preview-dialog.component.html',
})
export class PromptPreviewDialogComponent extends BaseComponent implements OnInit {
    private readonly _promptTemplateService = inject(PromptTemplateService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _data: IPromptPreviewDialogData = inject(DynamicDialogConfig).data;

    readonly isDirty = this._data.isDirty;
    blocks: { title: string; text: string }[] = [];

    override ngOnInit(): void {
        this.loading = true;
        this._promptTemplateService
            .preview(this._data.templateId)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.blocks = [
                        { title: 'Prompt hệ thống', text: res.data.systemPrompt },
                        { title: 'Prompt gửi kèm file', text: res.data.prompt },
                    ];
                }
            });
    }

    copy(text: string) {
        navigator.clipboard.writeText(text).then(
            () => this.messageSuccess('Đã sao chép'),
            () => this.messageError('Không sao chép được, hãy chọn và sao chép thủ công'),
        );
    }
}
