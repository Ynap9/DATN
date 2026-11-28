import { Component, computed, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { DOCUMENT_ENUM_PREVIEW_COUNT } from '@/shared/constants/document-field.constants';

// Ô "Giá trị" của bảng danh mục: vài giá trị đầu dạng chip, phần còn lại ghi "+n"
@Component({
    selector: 'app-enum-values-cell',
    imports: [TagModule],
    template: `
        <div class="flex flex-wrap items-center gap-1">
            @for (value of previewValues(); track value) {
                <p-tag severity="secondary" [value]="value" />
            }
            @if (hiddenCount() > 0) {
                <span class="text-xs text-muted-color">+{{ hiddenCount() }}</span>
            }
        </div>
    `,
})
export class EnumValuesCellComponent {
    row = input.required<{ values: string[] }>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();

    readonly previewValues = computed(() => this.row().values.slice(0, DOCUMENT_ENUM_PREVIEW_COUNT));
    readonly hiddenCount = computed(() => this.row().values.length - DOCUMENT_ENUM_PREVIEW_COUNT);
}
