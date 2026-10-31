import { Pipe, PipeTransform } from '@angular/core';
import { BaseConsts } from '../shared/const/base.consts';

@Pipe({
    name: 'libAcceptFile',})
export class AcceptFilePipe implements PipeTransform {

    transform(type: string): unknown {
        let accept: string;
        switch (type) {
            case 'image':
                accept = BaseConsts.imageExtensionStrings;
                break;
            case 'video':
                accept = BaseConsts.videoExtensionStrings;
                break;
            case 'media':
                accept = BaseConsts.imageExtensionStrings + ',' + BaseConsts.videoExtensionStrings;
                break;
            default:
                accept = type || '';
        }
        return accept;
    }

}
