import { ChangeDetectorRef, Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'labelDropdown',
    standalone: false
})
export class LabelDropdownPipe implements PipeTransform {

    constructor(
        private detectorRef: ChangeDetectorRef,
    ) {

    }
    transform(options: any[], [optionValue, optionLabel, value]): unknown {
        if (options.length) {
            const option = options.find(o => o[optionValue] === value);
            return option?.[optionLabel];
        }
        return null;
    }
}
