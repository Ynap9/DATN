import { Component, input } from '@angular/core';
import { ITableBadge } from '@/shared/models/data-table.models';

@Component({
    selector: 'app-status-badge',
    template: `<span [class]="'status-badge status-badge--' + badge().tone">{{ badge().label }}</span>`,
    styleUrl: './status-badge.component.scss',
})
export class StatusBadge {
    badge = input.required<ITableBadge>();
}
