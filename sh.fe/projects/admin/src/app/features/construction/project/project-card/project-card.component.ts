import { formatDate } from '@angular/common';
import { Component, computed, inject, input, LOCALE_ID, output } from '@angular/core';
import { CurrencyPipe } from '@sh/ui';
import { IViewProjectPair } from '@/models/project.model';
import { StatusBadge } from '@/shared/components/status-badge/status-badge.component';
import { TblAction } from '@/shared/components/tbl-action/tbl-action.component';
import { getProjectStatusBadge } from '@/shared/constants/project.constants';
import { ITableAction } from '@/shared/models/data-table.models';

@Component({
    selector: 'app-project-card',
    imports: [CurrencyPipe, StatusBadge, TblAction],
    templateUrl: './project-card.component.html',
    styleUrl: './project-card.component.scss',
})
export class ProjectCardComponent {
    private readonly _locale = inject(LOCALE_ID);

    project = input.required<IViewProjectPair>();
    actions = input.required<ITableAction[]>();

    open = output<void>();
    action = output<string>();

    statusBadge = computed(() => getProjectStatusBadge(this.project().status));
    // "01/11 – 30/12/2026": ngày bắt đầu bỏ năm khi cùng năm với ngày kết thúc; không có ngày nào ⇒ "—"
    period = computed(() => {
        const { startedAt, endedAt } = this.project();
        if (!startedAt && !endedAt) {
            return '—';
        }
        const sameYear = !!startedAt && !!endedAt && startedAt.slice(0, 4) === endedAt.slice(0, 4);
        const start = startedAt ? formatDate(startedAt, sameYear ? 'dd/MM' : 'dd/MM/yyyy', this._locale) : '—';
        const end = endedAt ? formatDate(endedAt, 'dd/MM/yyyy', this._locale) : '—';
        return `${start} – ${end}`;
    });
    progress = computed(() => Math.round(Math.min(Math.max(this.project().progressPercent ?? 0, 0), 100)));
}
