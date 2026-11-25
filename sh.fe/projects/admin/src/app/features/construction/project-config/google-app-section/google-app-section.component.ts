import { Component, DestroyRef, effect, inject, input, output, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { required } from '@sh/ui';
import { finalize } from 'rxjs';
import { ProjectConfigService } from '@/services/project-config.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { GOOGLE_CALLBACK_PATH, PROJECT_CONFIG_MASKED, PROJECT_CONFIG_NAME } from '@/shared/constants/project-config.constants';
import { SharedImports } from '@/shared/import.shared';
import { getSaveErrorMessage, IProjectConfigChange, ProjectConfigMap, saveProjectConfigs } from '../project-config-save';

// Ô của form → Name dòng cấu hình, theo thứ tự gửi khi Lưu
const FIELD_CONFIG_NAMES = {
    clientId: PROJECT_CONFIG_NAME.GOOGLE_CLIENT_ID,
    clientSecret: PROJECT_CONFIG_NAME.GOOGLE_CLIENT_SECRET,
    redirectUri: PROJECT_CONFIG_NAME.GOOGLE_REDIRECT_URI,
    folderId: PROJECT_CONFIG_NAME.GOOGLE_FOLDER_ID,
} as const;

type GoogleAppField = keyof typeof FIELD_CONFIG_NAMES;

const FIELDS = Object.keys(FIELD_CONFIG_NAMES) as GoogleAppField[];

// Đổi một trong ba dòng này khi đang kết nối thì Google cần cấp quyền lại (50.7)
const REAUTHORIZE_NAMES: string[] = [PROJECT_CONFIG_NAME.GOOGLE_CLIENT_ID, PROJECT_CONFIG_NAME.GOOGLE_CLIENT_SECRET, PROJECT_CONFIG_NAME.GOOGLE_REDIRECT_URI];

// Khối Ứng dụng Google: 4 dòng Client ID / secret / Redirect URI / Thư mục Drive (50.6, 50.7)
@Component({
    selector: 'app-google-app-section',
    imports: [SharedImports],
    templateUrl: './google-app-section.component.html',
})
export class GoogleAppSectionComponent extends BaseComponent {
    private readonly _projectConfigService = inject(ProjectConfigService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    configs = input.required<ProjectConfigMap>();
    isConnected = input.required<boolean>();
    // Lưu xong (kể cả dừng giữa chừng): màn cha tải lại để có id dòng vừa tạo
    saved = output<void>();

    readonly callbackPath = GOOGLE_CALLBACK_PATH;
    readonly canUpdate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE);
    private readonly _canCreate = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CONFIG_CREATE);

    override form = new FormGroup({
        clientId: new FormControl('', { nonNullable: true, validators: [required()] }),
        clientSecret: new FormControl('', { nonNullable: true, validators: [required()] }),
        redirectUri: new FormControl('', { nonNullable: true, validators: [required()] }),
        folderId: new FormControl('', { nonNullable: true, validators: [required()] }),
    });

    isChangingSecret = false;
    saving = false;
    // Giá trị đã tải; secret luôn rỗng vì BE không trả lại
    private _baseline: Record<GoogleAppField, string> = { clientId: '', clientSecret: '', redirectUri: '', folderId: '' };

    get hasSavedSecret() {
        return this.configs()[PROJECT_CONFIG_NAME.GOOGLE_CLIENT_SECRET]?.value === PROJECT_CONFIG_MASKED;
    }

    get isSecretEditing() {
        return !this.hasSavedSecret || this.isChangingSecret;
    }

    get hasChanges() {
        return this.getChanges().length > 0;
    }

    // Không chặn Lưu, chỉ nhắc (50.6)
    get isRedirectUriSuspicious() {
        const value = this.form.controls.redirectUri.value.trim();
        return !!value && !value.includes(GOOGLE_CALLBACK_PATH);
    }

    // Lần sửa / tạo gần nhất của 4 dòng; dòng tạo bằng POST chưa có modifiedDate
    get lastModifiedDate() {
        const dates = FIELDS.map((field) => this.configs()[FIELD_CONFIG_NAMES[field]])
            .map((row) => row?.modifiedDate ?? row?.createdDate)
            .filter((date): date is string => !!date);
        return dates.length ? dates.reduce((latest, date) => (new Date(date) > new Date(latest) ? date : latest)) : null;
    }

    constructor() {
        super();
        effect(() => {
            const configs = this.configs();
            untracked(() => this.applyConfigs(configs));
        });
    }

    isRequiredError(field: GoogleAppField) {
        const control = this.form.controls[field];
        return control.touched && control.hasError('required');
    }

    startChangeSecret() {
        this.isChangingSecret = true;
        this.form.controls.clientSecret.reset();
        this.updateEditable();
    }

    cancelChangeSecret() {
        this.isChangingSecret = false;
        this.form.controls.clientSecret.reset();
        this.updateEditable();
    }

    discard() {
        this.isChangingSecret = false;
        this.form.reset(this._baseline);
        this.updateEditable();
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
                if (this.isResponseSucceed(res, false, 'Đã lưu cấu hình Google')) {
                    if (this.isConnected() && changes.some((change) => REAUTHORIZE_NAMES.includes(change.name))) {
                        this.messageWarning('Nên bấm Kết nối lại để Google cấp quyền theo cấu hình mới');
                    }
                    this.cancelChangeSecret();
                } else {
                    this.messageError(getSaveErrorMessage(res));
                }
                this.saved.emit();
            });
    }

    // Ô chưa sửa nhận giá trị mới, ô đang sửa giữ nguyên: lưu dừng giữa chừng không mất phần chưa gửi
    private applyConfigs(configs: ProjectConfigMap) {
        const baseline = { ...this._baseline };
        for (const field of FIELDS) {
            const control = this.form.controls[field];
            const value = field === 'clientSecret' ? '' : (configs[FIELD_CONFIG_NAMES[field]]?.value ?? '');
            if (control.value.trim() === this._baseline[field]) control.setValue(value);
            baseline[field] = value;
        }
        this._baseline = baseline;
        this.updateEditable();
    }

    // Không có Update → chỉ đọc; dòng chưa có còn cần Create (50.1). Secret đã lưu mà chưa bấm Đổi thì không gửi
    private updateEditable() {
        for (const field of FIELDS) {
            const row = this.configs()[FIELD_CONFIG_NAMES[field]];
            const isEditable = this.canUpdate && (!!row || this._canCreate) && (field !== 'clientSecret' || this.isSecretEditing);
            const control = this.form.controls[field];
            if (isEditable) control.enable();
            else control.disable();
        }
    }

    // Ô bị khoá không tính; secret có nhập là đổi
    private getChanges(): IProjectConfigChange[] {
        return FIELDS.filter((field) => this.form.controls[field].enabled && this.form.controls[field].value.trim() !== this._baseline[field]).map((field) => {
            const name = FIELD_CONFIG_NAMES[field];
            return { name, value: this.form.controls[field].value.trim(), row: this.configs()[name] };
        });
    }
}
