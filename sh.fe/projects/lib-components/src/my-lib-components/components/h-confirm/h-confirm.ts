import { Component } from '@angular/core';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { IDialogConfirmConfig } from '../../shared/interfaces/base.interface';
import { FormBuilder, FormGroup } from '@angular/forms';
import { EFormatDateDisplay } from '../../shared/const/base.consts';
@Component({
    selector: 'h-confirm',
    standalone: false,
    templateUrl: './h-confirm.html',
    styleUrl: './h-confirm.scss',
})
export class HConfirmComponent {
    constructor(
        public ref: DynamicDialogRef,
        public configDialog: DynamicDialogConfig,
    ) { }

    EFormatDateDisplay = EFormatDateDisplay;
    styleIcon = { 'border-radius': '8px', 'width': '60px' };
    dialogData: IDialogConfirmConfig;

    form: FormGroup;

    ngOnInit(): void {
        let data = this.configDialog.data;
        this.dialogData = {
            ...data,
            icon: data?.icon,
        };
    }

    accept() {
        this.ref.close(true);
    }

    cancel() {
        this.ref.close();
    }
}
