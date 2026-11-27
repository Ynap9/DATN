import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ShCalendarComponent, ShInputTextComponent, ShMultiSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IViewProject } from '@/models/project.model';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PROJECT_STATUS, PROJECT_STATUSES } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';

const EXPIRED_PASSED_HINT = 'Dự án đã hết hạn. Chọn ngày kết thúc từ hôm nay trở đi, hoặc để trống, để gia hạn.';
const EXPIRED_RENEWED_HINT = 'Lưu xong dự án về Đang triển khai.';
const DOING_PASSED_HINT = 'Ngày kết thúc đã qua, sáng mai hệ thống chuyển dự án sang Hết hạn.';
const DATE_KEY_LENGTH = 'YYYY-MM-DD'.length;

// Ngày là chuỗi local 'YYYY-MM-DDT00:00:00' của sh-calendar nên so chuỗi là đủ
function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
    const { startedAt, endedAt } = group.value;
    return startedAt && endedAt && startedAt > endedAt ? { dateRange: true } : null;
}

@Component({
    selector: 'app-project-form',
    imports: [SharedImports, MessageModule, TagModule, ToggleSwitchModule, ShInputTextComponent, ShCalendarComponent, ShMultiSelectComponent],
    templateUrl: './project-form.component.html',
})
export class ProjectFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _projectService = inject(ProjectService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly project: IViewProject | undefined = this._config.data?.project;
    readonly documentTypes: IDocumentTypeDropdown[] = this._config.data?.documentTypes ?? [];
    readonly projectStatus = PROJECT_STATUSES.find((status) => status.value === this.project?.status);
    submitting = false;

    override form = new FormGroup(
        {
            name: new FormControl(this.project?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
            startedAt: new FormControl<string | null>(this.project?.startedAt ?? null),
            endedAt: new FormControl<string | null>(this.project?.endedAt ?? null),
            // Paging dự án dựa trên ProjectDocumentType: dự án không có metadata sẽ không hiện ở danh sách
            documentTypeIds: new FormControl<number[]>(this.project?.documentTypeIds ?? [], { nonNullable: true, validators: [Validators.required] }),
            isQaEnabled: new FormControl(this.project?.isQaEnabled ?? true, { nonNullable: true }),
            // Chỉ gửi khi tạo; form Sửa không có ô này
            isGoogleSheetEnabled: new FormControl(true, { nonNullable: true }),
        },
        { validators: dateRangeValidator },
    );

    get hasRemovedDocumentTypes() {
        const selectedIds = this.form.controls.documentTypeIds.value;
        return !!this.project?.documentTypeIds.some((id) => !selectedIds.includes(id));
    }

    // Hết hạn ↔ Đang triển khai do ngày kết thúc quyết định (BE Task 68); "hôm nay" theo ngày máy, so chuỗi 'YYYY-MM-DD' vì ngày là chuỗi local
    get endedAtHint() {
        const endedAt = this.form.controls.endedAt.value;
        const isEndedAtPassed = !!endedAt && endedAt.slice(0, DATE_KEY_LENGTH) < new Date().toLocaleDateString('sv-SE');
        if (this.project?.status === PROJECT_STATUS.EXPIRED) {
            return isEndedAtPassed ? { text: EXPIRED_PASSED_HINT, isWarning: true } : { text: EXPIRED_RENEWED_HINT, isWarning: false };
        }
        return this.project?.status === PROJECT_STATUS.DOING && isEndedAtPassed ? { text: DOING_PASSED_HINT, isWarning: true } : null;
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const { name, startedAt, endedAt, documentTypeIds, isQaEnabled, isGoogleSheetEnabled } = this.form.getRawValue();
        const request$ = this.project
            ? this._projectService.update(this.project.id, { name, startedAt, endedAt, status: this.project.status, documentTypeIds })
            : this._projectService.create({ name, startedAt, endedAt, isQaEnabled, isGoogleSheetEnabled, documentTypeIds });

        this.submitting = true;
        request$
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Lưu dự án thành công')) {
                    this._ref.close(true);
                }
            });
    }
}
