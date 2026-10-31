import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { ShInputSearchComponent } from '../sh-input-search/sh-input-search';

@Component({
    selector: 'sh-dropdown-search',
    imports: [FormsModule, SelectModule, ShInputSearchComponent],
    templateUrl: './sh-dropdown-search.html',
    styleUrl: './sh-dropdown-search.scss',
})
export class ShDropdownSearchComponent {
    @Input() filterArray: any[];
    @Input() dataFilter: {
        keyword: string;
        keywordType: string;
    };
    @Input() optionLabel: string = "name";
    @Input() optionValue: string = "field";

    @Output() _onChange = new EventEmitter<object>();

    onChange() {
        const emitValue = {
            [this.dataFilter.keywordType]: this.dataFilter.keyword
        };
        this._onChange.emit(emitValue);
    }
}
