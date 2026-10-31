import { Component } from '@angular/core';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { IColumn, IEventChecked } from '../../../shared/interfaces/sh-table.interface';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { CheckboxModule } from 'primeng/checkbox';

@Component({
    selector: 'sh-setting-display-column-table',
    imports: [CommonModule, FormsModule, ButtonModule, RippleModule, CheckboxModule],
    templateUrl: './sh-setting-display-column-table.html',
    styleUrl: './sh-setting-display-column-table.scss',
})
export class ShSettingDisplayColumnTableComponent {
    constructor(
        public ref: DynamicDialogRef,
        public configDialog: DynamicDialogConfig
    ) { }
    title: string;
    submitted = false;
    acceptStatus: boolean = true;

    selectColumns: IColumn[] = [];
    isCheckedAll: boolean = true;
    isPinAll: boolean = true;
    showReset: boolean = false;


    ngOnInit(): void {
        this.showReset = !!this.configDialog.data?.showReset;
        this.selectColumns = JSON.parse(JSON.stringify(this.configDialog.data.cols));
        this.checkIsCheckedAll();
        this.selectColumns.forEach((col) => {
            if (col.displaySettingColumn) {
                if (!col.isPin) {
                    this.isPinAll = false;
                }
            }
        })
    }

    countColumnShow() {
        const columnDisplay = this.selectColumns.filter(s => s?.displaySettingColumn);
        return `(${columnDisplay?.length} cột)`;
    }

    checkIsCheckedAll() {
        let countSeletedDisplay = this.selectColumns.filter(s => s.displaySettingColumn)?.length;
        let countSeletedShow = this.selectColumns.filter(s => s.isShow && s.displaySettingColumn)?.length;
        this.isCheckedAll = (countSeletedDisplay === countSeletedShow);
    }

    changeSelectedAll(event: any) {
        this.selectColumns.forEach(item => {
            if (item.displaySettingColumn && !item?.isPin) {
                item.isShow = event.checked;
            }
        });
    }

    accept() {
        this.acceptStatus = true;
        this.onAccept();
    }

    cancel() {
        this.acceptStatus = false;
        this.onAccept();
    }

    resetDefault() {
        this.ref.close({ reset: true });
    }

    onAccept() {
        this.selectColumns = this.selectColumns.sort((a, b) => a.position - b.position);
        this.ref.close({ data: this.selectColumns, accept: this.acceptStatus });
    }
}
