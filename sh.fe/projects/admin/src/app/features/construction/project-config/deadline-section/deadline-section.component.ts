import { Component, DestroyRef, effect, inject, input, output, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { finalize } from 'rxjs';
import { ProjectConfigService } from '@/services/project-config.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { DEADLINE_WARNING_DAYS_DEFAULT, PROJECT_CONFIG_NAME } from '@/shared/constants/project-config.constants';
import { SharedImports } from '@/shared/import.shared';
import { getSaveErrorMessage, IProjectConfigChange, ProjectConfigMap, saveProjectConfigs } from '../project-config-save';

// Khối Hạn dự án: số ngày báo trước + bật / tắt mail báo hạn (50.8)
@Component({
    selector: 'app-deadline-section',
    imports: [SharedImports, CheckboxModule],
    templateUrl: './deadline-section.component.html',
})
export class DeadlineSectionComponent extends BaseComponent {
    private readonly _projectConfigService = inject(ProjectConfigService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    configs = input.required<ProjectConfigMap>();
    // Lưu xong (kể cả dừng giữa chừng): màn cha tải lại để có id dòng vừa tạo
    saved = output<void>();

    readonly warningDaysDefault = DEADLINE_WARNING_DAYS_DEFAULT;
    readonly canUpdate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE);
    private readonly _canCreate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CONFIG_CREATE);

    // Trống được (BE dùng mặc định); có nhập thì phải là số nguyên ≥ 0
    override form = new FormGroup({
        warningDays: new FormControl('', { nonNullable: true, validators: [Validators.pattern(/^\s*\d+\s*$/)] }),
        mailEnabled: new FormControl(true, { nonNullable: true }),
    });

    saving = false;
    private _baseline = { warningDays: '', mailEnabled: true };

    get hasChanges() {
        return this.getChanges().length > 0;
    }

    get isWarningDaysInvalid() {
        const control = this.form.controls.warningDays;
        return control.touched && control.invalid;
    }

    constructor() {
        super();
        effect(() => {
            const configs = this.configs();
            untracked(() => this.applyConfigs(configs));
        });
    }

    save() {
        if (this.isFormInvalid()) return;
        const changes = this.getChanges();
        if (!changes.length) return;

        this.saving = true;
        saveProjectConfigs(this._projectConfigService, changes)
            .pipe(
                finalize(() => (this.saving = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res, false, 'Đã lưu cấu hình hạn dự án')) {
                    this.messageError(getSaveErrorMessage(res));
                }
                this.saved.emit();
            });
    }

    // Ô chưa sửa nhận giá trị mới, ô đang sửa giữ nguyên. Mail: chưa có dòng hoặc khác "false" là bật (BE mặc định true)
    private applyConfigs(configs: ProjectConfigMap) {
        const baseline = {
            warningDays: configs[PROJECT_CONFIG_NAME.DEADLINE_WARNING_DAYS]?.value.trim() ?? '',
            mailEnabled: configs[PROJECT_CONFIG_NAME.DEADLINE_MAIL_ENABLED]?.value.trim().toLowerCase() !== 'false',
        };
        const { warningDays, mailEnabled } = this.form.controls;
        if (warningDays.value.trim() === this._baseline.warningDays) warningDays.setValue(baseline.warningDays);
        // Giá trị sai sẵn trong DB (BE đang dùng mặc định) thì báo lỗi tại ô ngay
        if (warningDays.invalid) warningDays.markAsTouched();
        if (mailEnabled.value === this._baseline.mailEnabled) mailEnabled.setValue(baseline.mailEnabled);
        this._baseline = baseline;
        this.updateEditable();
    }

    // Không có Update → chỉ đọc; dòng chưa có còn cần Create (50.1)
    private updateEditable() {
        const controls = [
            { control: this.form.controls.warningDays, name: PROJECT_CONFIG_NAME.DEADLINE_WARNING_DAYS },
            { control: this.form.controls.mailEnabled, name: PROJECT_CONFIG_NAME.DEADLINE_MAIL_ENABLED },
        ];
        for (const { control, name } of controls) {
            if (this.canUpdate && (!!this.configs()[name] || this._canCreate)) control.enable();
            else control.disable();
        }
    }

    // Ô trống gửi ""; mail gửi "true" / "false"
    private getChanges(): IProjectConfigChange[] {
        const { warningDays, mailEnabled } = this.form.controls;
        const changes: IProjectConfigChange[] = [];
        if (warningDays.enabled && warningDays.value.trim() !== this._baseline.warningDays) {
            const name = PROJECT_CONFIG_NAME.DEADLINE_WARNING_DAYS;
            changes.push({ name, value: warningDays.value.trim(), row: this.configs()[name] });
        }
        if (mailEnabled.enabled && mailEnabled.value !== this._baseline.mailEnabled) {
            const name = PROJECT_CONFIG_NAME.DEADLINE_MAIL_ENABLED;
            changes.push({ name, value: String(mailEnabled.value), row: this.configs()[name] });
        }
        return changes;
    }
}
