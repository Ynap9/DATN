import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'labelDropdown',})
export class LabelDropdownPipe implements PipeTransform {

    transform(options: any[], [optionValue, optionLabel, value]): unknown {
        if (options.length) {
            const option = options.find(o => o[optionValue] === value);
            return option?.[optionLabel];
        }
        return null;
    }
}
