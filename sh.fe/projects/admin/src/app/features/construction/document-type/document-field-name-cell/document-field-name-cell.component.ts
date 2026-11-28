import { Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';

// Ô "Tên trường" của bảng Trường: kèm chip "Danh mục: {tên}" khi trường chọn từ danh mục
@Component({
    selector: 'app-document-field-name-cell',
    imports: [TagModule],
    template: `
        <div class="flex flex-wrap items-center gap-2">
            <span>{{ row().name }}</span>
            @if (row().enumName) {
                <p-tag severity="info" [value]="'Danh mục: ' + row().enumName" />
            }
        </div>
    `,
})
export class DocumentFieldNameCellComponent {
    row = input.required<{ name: string; enumName?: string }>();
    // DataTable CUSTOM_COMP luôn truyền đủ { row, rowIndex, data }
    rowIndex = input<number>();
    data = input<unknown>();
}
