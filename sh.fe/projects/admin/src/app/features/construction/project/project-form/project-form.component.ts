import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ShCalendarComponent, ShInputTextComponent, ShMultiSelectComponent, ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageModule } from 'primeng/message';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { IViewProject } from '@/models/project.model';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PROJECT_STATUS, PROJECT_STATUSES } from '@/shared/constants/project.constants';
import { SharedImports } from '@/shared/import.shared';

const EDITABLE_STATUSES: number[] = [PROJECT_STATUS.DOING, PROJECT_STATUS.EXPIRED];

// Ngày là chuỗi local 'YYYY-MM-DDT00:00:00' của sh-calendar nên so chuỗi là đủ
function dateRangeValidator(group: AbstractControl): ValidationErrors | null {
    const { startedAt, endedAt } = group.value;
    return startedAt && endedAt && startedAt > endedAt ? { dateRange: true } : null;
}

@Component({
    selector: 'app-project-form',
    imports: [SharedImports, MessageModule, ToggleSwitchModule, ShInputTextComponent, ShCalendarComponent, ShMultiSelectComponent, ShSelectComponent],
    templateUrl: './project-form.component.html',
})
export class ProjectFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _projectService = inject(ProjectService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly project: IViewProject | undefined = this._config.data?.project;
    readonly documentTypes: IDocumentTypeDropdown[] = this._config.data?.documentTypes ?? [];
    readonly isStatusLocked = !!this.project && !EDITABLE_STATUSES.includes(this.project.status);
    readonly statuses = this.isStatusLocked ? PROJECT_STATUSES : PROJECT_STATUSES.filter((status) => EDITABLE_STATUSES.includes(status.value));
    submitting = false;

    override form = new FormGroup(
        {
            name: new FormControl(this.project?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
            startedAt: new FormControl<string | null>(this.project?.startedAt ?? null),
            endedAt: new FormControl<string | null>(this.project?.endedAt ?? null),
            // Paging dự án dựa trên ProjectDocumentType: dự án không có metadata sẽ không hiện ở danh sách
            documentTypeIds: new FormControl<number[]>(this.project?.documentTypeIds ?? [], { nonNullable: true, validators: [Validators.required] }),
            isQaEnabled: new FormControl(this.project?.isQaEnabled ?? true, { nonNullable: true }),
            status: new FormControl({ value: this.project?.status ?? PROJECT_STATUS.DOING, disabled: this.isStatusLocked }, { nonNullable: true }),
        },
        { validators: dateRangeValidator },
    );

    get hasRemovedDocumentTypes() {
        const selectedIds = this.form.controls.documentTypeIds.value;
        return !!this.project?.documentTypeIds.some((id) => !selectedIds.includes(id));
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        const { name, startedAt, endedAt, documentTypeIds, isQaEnabled, status } = this.form.getRawValue();
        const request$ = this.project
            ? this._projectService.update(this.project.id, { name, startedAt, endedAt, status, documentTypeIds })
            : this._projectService.create({ name, startedAt, endedAt, isQaEnabled, documentTypeIds });

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
