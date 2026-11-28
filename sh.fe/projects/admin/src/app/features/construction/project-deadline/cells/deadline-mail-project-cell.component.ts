import { Component, input } from '@angular/core';
import { IProjectDeadlineNotification } from '@/models/project-deadline.model';

// Tên dự án lúc gửi mail, dòng dưới là tiêu đề mail
@Component({
    selector: 'app-deadline-mail-project-cell',
    template: `
        <div class="font-semibold">{{ row().projectName }}</div>
        <div class="text-xs text-[var(--app-muted)]">{{ row().subject }}</div>
    `,
})
export class DeadlineMailProjectCellComponent {
    row = input.required<IProjectDeadlineNotification>();
    rowIndex = input<number>();
    data = input<unknown>();
}
