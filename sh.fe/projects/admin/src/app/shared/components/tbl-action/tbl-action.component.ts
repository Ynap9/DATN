import { Component, computed, input, output } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { ITableAction } from '@/shared/models/data-table.models';

@Component({
    selector: 'app-tbl-action',
    imports: [ButtonModule, MenuModule],
    templateUrl: './tbl-action.component.html',
})
export class TblAction {
    actions = input.required<ITableAction[]>();

    action = output<string>();

    readonly menuTokens = {
        list: { padding: '6px', gap: '4px' },
        item: { padding: '8px 12px', borderRadius: '6px', gap: '10px' },
    };

    menuItems = computed<MenuItem[]>(() =>
        this.actions().map((item) => ({
            label: item.label,
            icon: item.icon,
            state: { danger: item.severity === 'danger' },
            command: () => this.action.emit(item.type),
        })),
    );
}
