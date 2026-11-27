import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { IProjectDeadline } from '@/models/project-deadline.model';

// Mail báo hạn thành công gần nhất của dự án; chưa gửi thì chữ xám
@Component({
    selector: 'app-deadline-last-mail-cell',
    imports: [DatePipe],
    template: `
        @if (row().lastNotifiedDate; as notifiedDate) {
            {{ notifiedDate | date: 'dd/MM/yyyy HH:mm' }}
        } @else {
            <span class="text-[var(--app-muted)]">Chưa gửi</span>
        }
    `,
})
export class DeadlineLastMailCellComponent {
    row = input.required<IProjectDeadline>();
    rowIndex = input<number>();
    data = input<unknown>();
}
