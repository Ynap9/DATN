import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'function',
    standalone: false
})
export class FunctionPipe implements PipeTransform {

    transform(callBackFunc: Function, params: any[]): any {
        if (typeof callBackFunc === 'function') {
            if (Array.isArray(params)) return callBackFunc.apply(this, params);
            if (params) return callBackFunc(params);
            return callBackFunc();
        }
        return false;
    }

}
