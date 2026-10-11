import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'h-dropdown-search',
    standalone: false,
    templateUrl: './h-dropdown-search.html',
    styleUrl: './h-dropdown-search.scss',
})
export class HDropdownSearch {
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
