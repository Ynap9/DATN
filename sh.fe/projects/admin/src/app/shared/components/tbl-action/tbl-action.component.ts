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

    menuItems = computed<MenuItem[]>(() =>
        this.actions().map((item) => ({
            label: item.label,
            icon: item.icon,
            command: () => this.action.emit(item.type),
        })),
    );
}
