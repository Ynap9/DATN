import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { IColumnSetting } from '@/shared/models/data-table.models';

export interface IColumnSettingItem extends IColumnSetting {
    header: string;
}

export interface IColumnSettingsDialogData {
    items: IColumnSettingItem[];
    defaults: IColumnSettingItem[];
    lockedHeaders: string[];
}

@Component({
    selector: 'app-data-table-column-settings',
    imports: [FormsModule, ButtonModule, CheckboxModule, TableModule],
    templateUrl: './data-table-column-settings.component.html',
    styleUrl: './data-table-column-settings.component.scss',
})
export class DataTableColumnSettings {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IColumnSettingsDialogData = inject(DynamicDialogConfig).data;

    readonly lockedHeaders = this._data.lockedHeaders;
    items = this._data.items.map((item) => ({ ...item }));

    get checkedCount(): number {
        return this.items.filter((item) => item.visible).length;
    }

    toggleAll(visible: boolean): void {
        this.items.forEach((item) => (item.visible = visible));
    }

    reset(): void {
        this.items = this._data.defaults.map((item) => ({ ...item }));
    }

    close(): void {
        this._ref.close();
    }

    save(): void {
        this._ref.close(this.items.map(({ key, visible }): IColumnSetting => ({ key, visible })));
    }
}
