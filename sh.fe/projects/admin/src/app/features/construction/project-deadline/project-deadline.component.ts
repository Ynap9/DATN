import { Component, DestroyRef, inject, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Params } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { exhaustMap, finalize, take, takeWhile, timer } from 'rxjs';
import { IDeadlineCheckStatus, IDeadlineProjectFilter } from '@/models/project-deadline.model';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PageHeader } from '@/shared/components/page-header/page-header.component';
import {
    DEADLINE_JOB_STATE,
    DEADLINE_POLL_INTERVAL_MS,
    DEADLINE_POLL_MAX_TIMES,
    DEADLINE_TAB,
    DEADLINE_TABS,
    DeadlineTab,
    isDeadlineJobRunning,
} from '@/shared/constants/project-deadline.constants';
import { SharedImports } from '@/shared/import.shared';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { DeadlineAttentionTabComponent } from './deadline-attention-tab/deadline-attention-tab.component';
import { DeadlineMailTabComponent } from './deadline-mail-tab/deadline-mail-tab.component';
import { DEADLINE_QUERY, readDeadlineQuery, updateDeadlineQuery } from './deadline-query';
import { DeadlineStatusCardComponent } from './deadline-status-card/deadline-status-card.component';

// Màn Hạn dự án: trạng thái job kiểm hạn, dự án sắp / đã hết hạn, lịch sử mail báo hạn (Task 73)
@Component({
    selector: 'app-project-deadline',
    imports: [SharedImports, PageHeader, DeadlineStatusCardComponent, DeadlineAttentionTabComponent, DeadlineMailTabComponent],
    templateUrl: './project-deadline.component.html',
})
export class ProjectDeadlineComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _destroyRef = inject(DestroyRef);
    // Chỉ tab đang mở được dựng nên tab kia là undefined
    private readonly _attentionTab = viewChild(DeadlineAttentionTabComponent);
    private readonly _mailTab = viewChild(DeadlineMailTabComponent);

    readonly tabs = DEADLINE_TABS;
    readonly tabKeys = DEADLINE_TAB;
    readonly breadcrumbs: MenuItem[] = [{ label: 'Hồ sơ thi công' }, { label: 'Hạn dự án' }];
    // null = đang tải
    status: IDeadlineCheckStatus | null = null;
    isRunning = false;
    // Tab đang mở nằm trên URL (?tab=mails), tab đầu không ghi tham số
    activeTab: DeadlineTab = DEADLINE_TAB.ATTENTION;

    override ngOnInit(): void {
        this._activatedRoute.queryParamMap
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((params) => (this.activeTab = readDeadlineQuery(params).tab));
        this.loadStatus();
    }

    loadStatus() {
        this._projectService
            .findDeadlineCheckStatus()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.status = res.data;
                }
            });
    }

    // Đổi tab thì bỏ bộ lọc của tab cũ
    selectTab(tab: DeadlineTab) {
        if (tab !== this.activeTab) {
            this.openTab(tab, {});
        }
    }

    showMails(project: IDeadlineProjectFilter) {
        this.openTab(DEADLINE_TAB.MAILS, { [DEADLINE_QUERY.PROJECT]: project.projectId, [DEADLINE_QUERY.PROJECT_NAME]: project.name });
    }

    private openTab(tab: DeadlineTab, filters: Params) {
        const cleared = Object.fromEntries(Object.values(DEADLINE_QUERY).map((key) => [key, null]));
        updateDeadlineQuery(this.router, this._activatedRoute, {
            ...cleared,
            ...filters,
            [DEADLINE_QUERY.TAB]: tab === DEADLINE_TAB.ATTENTION ? null : tab,
        });
    }

    refresh() {
        this.loadStatus();
        this.reloadTabs();
    }

    private reloadTabs() {
        this._attentionTab()?.load();
        this._mailTab()?.load();
    }

    // Chưa đăng ký job thì BE từ chối (400), nên khoá nút từ trước
    get canRun() {
        return !!this.status?.job && !this.isRunning;
    }

    confirmRun() {
        if (!this.status) return;
        const mailNote = this.status.mailEnabled ? ` Mail báo hạn sẽ gửi cho ${this.status.recipients.length} người.` : '';
        this.confirmAction(
            { header: 'Chạy kiểm hạn ngay?', message: `Dự án quá ngày kết thúc sẽ chuyển sang Hết hạn.${mailNote}` },
            () => this.runCheck(),
        );
    }

    private runCheck() {
        this.isRunning = true;
        this._projectService
            .runDeadlineCheck()
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.pollJob(res.data.jobId);
                    } else {
                        this.isRunning = false;
                    }
                },
                error: () => (this.isRunning = false),
            });
    }

    // Hỏi lại trạng thái tới khi lần chạy gần nhất đúng là job vừa tạo và đã hết Enqueued / Processing
    private pollJob(jobId: string) {
        let isDone = false;
        timer(DEADLINE_POLL_INTERVAL_MS, DEADLINE_POLL_INTERVAL_MS)
            .pipe(
                take(DEADLINE_POLL_MAX_TIMES),
                exhaustMap(() => this._projectService.findDeadlineCheckStatus()),
                takeWhile((res) => !this.isJobFinished(res, jobId), true),
                takeUntilDestroyed(this._destroyRef),
                finalize(() => (this.isRunning = false)),
            )
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res, false)) return;
                    this.status = res.data;
                    if (this.isJobFinished(res, jobId)) {
                        isDone = true;
                        this.notifyJobResult(res.data);
                        this.reloadTabs();
                    }
                },
                complete: () => {
                    if (!isDone) {
                        this._messageService.add({ closable: true, severity: 'info', detail: 'Job vẫn đang chạy, bấm Làm mới sau ít phút', life: 4000 });
                    }
                },
            });
    }

    private isJobFinished(res: IBaseResponseWithData<IDeadlineCheckStatus>, jobId: string) {
        const job = res.data?.job;
        return res.status === 1 && job?.lastJobId === jobId && !isDeadlineJobRunning(job.lastJobState);
    }

    private notifyJobResult(status: IDeadlineCheckStatus) {
        const job = status.job;
        if (job?.lastJobState === DEADLINE_JOB_STATE.FAILED) {
            this.messageError(`Kiểm hạn thất bại${job.error ? `: ${job.error}` : ''}`);
        } else {
            this.messageSuccess('Đã kiểm hạn xong');
        }
    }
}
