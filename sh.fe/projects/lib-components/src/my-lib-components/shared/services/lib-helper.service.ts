import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Location } from '@angular/common';
import { DomSanitizer, SafeHtml, SafeUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { BaseConsts, ContentTypeEView, StatusResonse, StatusResponseConst } from '../const/base.consts';
import { IDialogConfirmConfig, IParamHandleDTO } from '../interfaces/base.interface';
import { HConfirmComponent } from '../../../public-api';
import { firstValueFrom, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class LibHelperService {
    constructor(
        public messageService: MessageService,
        public sanitizer: DomSanitizer,
        public dialogService: DialogService,
        public router: Router,
        public activatedRoute: ActivatedRoute,
        public location: Location,
        public http: HttpClient,
    ) { }

    exceptionCodeWarnings = [6005, 6006, 7012, 7013, 7055, 4106, 4107, 7095, 7094, 7099, 7100, 7105, 7106, 7107, 7108, 7113, 7114, 7115, 7116, 7084, 7120, 7140, 7141];
    exceptionErrorCodeNothingPass = [4104, 1031, 1003, 7120];

    handleResponseInterceptor(response: any, message?: string): boolean {
        if (response?.statusCode === StatusResponseConst.RESPONSE_TRUE) {
            if (message) {
                this.messageService.add({ severity: 'success', summary: '', detail: message, life: 1000 });
            } else if (response?.successFE && message === undefined) {
                this.messageService.add({ severity: 'success', summary: '', detail: response.successFE, life: 1000 });
            }
            return true;
        } else {
            if (this.exceptionCodeWarnings.includes(response.code)) {
                this.messageService.add({ severity: 'warn', summary: '', detail: response.message, life: 3000 });
                return true;
            } else if (this.exceptionErrorCodeNothingPass.includes(response.code)) {
                this.messageService.add({ severity: 'warn', summary: '', detail: response.message, life: 3000 });
                return false;
            }
            let dataMessage = response?.data;
            if (dataMessage && typeof dataMessage === 'object') {
                const message = dataMessage?.[0]?.value?.[0] || dataMessage?.[Object.keys(dataMessage)?.[0]];
                this.messageService.add({ severity: 'error', summary: '', detail: message, life: 2500 });
            } else {
                let message = response?.message;
                if (response?.code > 1 && response?.code < 1000) {
                    message = "Có lỗi xảy ra vui lòng thử lại sau!";
                }
                this.messageService.add({ severity: 'error', summary: '', detail: response?.message, life: 2500 });
            }
            return false;
        }
    }

    messageError(msg = '', summary = '', life = 2500) {
        this.messageService.add({ severity: 'error', summary, detail: msg, life: life });
    }

    messageSuccess(msg = '', summary = '', life = 2000) {
        this.messageService.add({ severity: 'success', summary, detail: msg, life: life });
    }

    messageWarn(msg = '', life = 3000) {
        this.messageService.add({ severity: 'warn', summary: '', detail: msg, life: life });
    }

    mapDTO(data: Object, model: Object, paramHanldes?: IParamHandleDTO) {
        paramHanldes = {
            idName: paramHanldes?.idName || 'id',
            isCheckNull: !!paramHanldes?.isCheckNull,
        }
        let newData: any = {};
        for (const key of Object.keys(model)) {
            if ((key === paramHanldes?.idName || paramHanldes.isCheckNull) && !data[key]) continue;
            newData[key] = data[key];
        }
        return newData;
    }

    getBlobUrlImage(image: File): SafeUrl {
        return this.sanitizer.bypassSecurityTrustUrl(image?.['objectURL']?.['changingThisBreaksApplicationSecurity']);
    }

    getContentHtml(content): SafeHtml {
        return this.sanitizer.bypassSecurityTrustHtml(content);
    }

    checkValidForm(isValid: boolean,) {
        if (!isValid) this.messageError('Vui lòng nhập đủ thông tin');
    }

    dialogConfirmRef(message: string, options?: IDialogConfirmConfig): DynamicDialogRef {
        return this.dialogService.open(
            HConfirmComponent,
            {
                header: options?.header || "Thông báo",
                width: options?.width || '560px',
                data: {
                    message: message,
                    ...(options || {}),
                },
            },
        );
    }

    downloadFile(url: string, unAuthorize = false): Observable<any> {
        return this.http.get(url, { responseType: 'blob', params: { unAuthorize: unAuthorize }, observe: 'response' });
    }

    downloadFilePost(url: string, data, unAuthorize = false): Observable<any> {
        return this.http.post(url, data, { responseType: 'blob', params: { unAuthorize: unAuthorize }, observe: 'response' });
    }

    checkDownload(url: string, unAuthorize = false): Observable<any> {
        return this.http.get(url, { params: { unAuthorize: unAuthorize } });
    }

    async awaitApi(api: Observable<any>) {
        try {
            const resData = await firstValueFrom(api);
            return resData;
        } catch (error) {
            console.error('Error:', error);
            return null;
        }
    }


    checkStatusResponse(response: any, message?: string): boolean {
        console.log(response)
        if (response?.statusCode === StatusResonse.SUCCESS) {
            if (message) {
                this.messageService.add({ severity: 'success', summary: '', detail: message, life: 1000 });
            } else if (response?.message && message === undefined) {
                this.messageService.add({ severity: 'success', summary: '', detail: response.message, life: 1000 });
            }
            return true;
        } else {
            let dataMessage = response?.data;
            if (dataMessage && typeof dataMessage === 'object') {
                const message = dataMessage?.[0]?.value?.[0] || dataMessage?.[Object.keys(dataMessage)?.[0]];
                this.messageService.add({ severity: 'error', summary: '', detail: message, life: 2500 });
            } else {
                let message = response?.message;
                if (response?.code > 1 && response?.code < 1000) {
                    message = BaseConsts.messageError;
                }
                this.messageService.add({ severity: 'error', summary: '', detail: response?.message, life: 2500 });
            }
            return false;
        }
    }

}
