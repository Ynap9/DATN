import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MenuItem } from 'primeng/api';
import { ProjectConfigService } from '@/services/project-config.service';
import { SharedService } from '@/services/shared.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { GOOGLE_CALLBACK_RESULT, PROJECT_CONFIG_NAME } from '@/shared/constants/project-config.constants';
import { SharedImports } from '@/shared/import.shared';
import { DeadlineSectionComponent } from './deadline-section/deadline-section.component';
import { GoogleAppSectionComponent } from './google-app-section/google-app-section.component';
import { GoogleConnectionSectionComponent, IGoogleConnectResult } from './google-connection-section/google-connection-section.component';
import { ProjectConfigMap, toProjectConfigMap } from './project-config-save';

const GOOGLE_CONNECTED_MESSAGE = 'Đã kết nối tài khoản Google. Từ giờ tạo dự án sẽ tạo sheet trong thư mục Drive đã cấu hình.';
// BE luôn gửi message khi lỗi; câu này chỉ phòng URL bị sửa tay
const GOOGLE_CONNECT_FAILED_MESSAGE = 'Kết nối Google thất bại, thử lại sau';

// Màn Cấu hình: kết nối Google, ứng dụng Google, hạn dự án — các dòng của bảng ProjectConfig (Task 50)
@Component({
    selector: 'app-project-config',
    imports: [SharedImports, PageHeader, GoogleConnectionSectionComponent, GoogleAppSectionComponent, DeadlineSectionComponent],
    templateUrl: './project-config.component.html',
})
export class ProjectConfigComponent extends BaseComponent {
    private readonly _projectConfigService = inject(ProjectConfigService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _googleAppSection = viewChild(GoogleAppSectionComponent);
    private readonly _deadlineSection = viewChild(DeadlineSectionComponent);

    readonly breadcrumbs: MenuItem[] = [{ label: 'Quản trị' }, { label: 'Cấu hình' }];
    // Lấy link Google cần quyền Update (50.1)
    readonly canConnect = this._sharedService.isGranted(PermissionConstants.FUNCTION_PROJECT_CONFIG_UPDATE);
    // null = chưa tải xong
    configs: ProjectConfigMap | null = null;
    connecting = false;
    googleResult: IGoogleConnectResult | null = null;

    // BE che refresh token có giá trị thành "******", rỗng thì trả rỗng (50.4)
    get isGoogleConnected() {
        return !!this.configs?.[PROJECT_CONFIG_NAME.GOOGLE_REFRESH_TOKEN]?.value;
    }

    override ngOnInit(): void {
        this.readGoogleResult();
        this.loadConfigs();
    }

    loadConfigs() {
        this._projectConfigService
            .getList()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.configs = toProjectConfigMap(res.data);
                }
            });
    }

    // Chuyển cả tab sang Google (không mở tab mới); khối nào còn sửa chưa lưu thì hỏi trước (50.4)
    async connectGoogle() {
        if (this.hasChanges && !(await this.confirmLeave('Có thay đổi chưa lưu. Rời trang để kết nối Google?'))) return;

        this.connecting = true;
        this._projectConfigService
            .getGoogleAuthorizationUrl()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        window.location.href = res.data;
                    } else {
                        this.connecting = false;
                    }
                },
                error: () => (this.connecting = false),
            });
    }

    // Rời màn khi còn sửa chưa lưu ở khối nào thì hỏi trước (50.9)
    canDeactivate(): boolean | Promise<boolean> {
        return this.hasChanges ? this.confirmLeave('Có thay đổi chưa lưu. Rời trang?') : true;
    }

    private get hasChanges() {
        return !!(this._googleAppSection()?.hasChanges || this._deadlineSection()?.hasChanges);
    }

    // Đọc ?google= rồi xoá khỏi URL để F5 không hiện lại (50.5)
    private readGoogleResult() {
        const query = this._activatedRoute.snapshot.queryParamMap;
        const google = query.get('google');
        if (google === GOOGLE_CALLBACK_RESULT.SUCCESS) {
            this.googleResult = { isSuccess: true, message: GOOGLE_CONNECTED_MESSAGE };
        } else if (google === GOOGLE_CALLBACK_RESULT.ERROR) {
            this.googleResult = { isSuccess: false, message: query.get('message') || GOOGLE_CONNECT_FAILED_MESSAGE };
        }
        if (google !== null) {
            this.router.navigate([], { relativeTo: this._activatedRoute, queryParams: {}, replaceUrl: true });
        }
    }

    private confirmLeave(message: string): Promise<boolean> {
        return new Promise((resolve) =>
            this._confirmationService.confirm({
                message,
                closable: true,
                closeOnEscape: true,
                rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
                acceptButtonProps: { label: 'Rời đi', severity: 'danger' },
                accept: () => resolve(true),
                reject: () => resolve(false),
            }),
        );
    }
}
