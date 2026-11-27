import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PopoverModule } from 'primeng/popover';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';
import { IDeadlineCheckStatus } from '@/models/project-deadline.model';
import { SharedService } from '@/services/shared.service';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PROJECT_CONFIG_URL } from '@/shared/constants/project-config.constants';
import { DEADLINE_JOB_STATE, getDeadlineJobStateTag } from '@/shared/constants/project-deadline.constants';
import { SharedImports } from '@/shared/import.shared';
import { formatDeadlineCron } from './deadline-cron';

// Thẻ trạng thái job kiểm hạn: lịch chạy, lần chạy gần nhất, người nhận mail (Task 73.4)
@Component({
    selector: 'app-deadline-status-card',
    imports: [SharedImports, RouterLink, PopoverModule, SkeletonModule, TagModule],
    templateUrl: './deadline-status-card.component.html',
})
export class DeadlineStatusCardComponent {
    private readonly _sharedService = inject(SharedService);

    // null = đang tải
    status = input<IDeadlineCheckStatus | null>(null);

    readonly configUrl = PROJECT_CONFIG_URL;
    readonly canOpenConfig = this._sharedService.isGranted(PermissionConstants.MENU_PROJECT_CONFIG);
    readonly cron = computed(() => formatDeadlineCron(this.status()?.job?.cron ?? ''));
    readonly jobStateTag = computed(() => getDeadlineJobStateTag(this.status()?.job?.lastJobState ?? null));
    readonly isJobFailed = computed(() => this.status()?.job?.lastJobState === DEADLINE_JOB_STATE.FAILED);
}
