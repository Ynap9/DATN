import { Injector } from '@angular/core';
import { ActiveDeactiveConst, BaseConsts, EActiveDeactive, EFormatDate, EIconConfirm, ErrorMessage, EYesNo, SuccessMessage } from './shared/const/base.consts';
import { TokenService } from './shared/services/auth/token.service';
import { Utils } from '../public-api';
import { LibHelperService } from './shared/services/lib-helper.service';
import { Page } from './shared/models/page';
import { ReplaySubject } from 'rxjs';
import moment from 'moment';
import { AbstractControl, FormArray, FormGroup } from '@angular/forms';

export abstract class HComponentBase {
    _libHelperService: LibHelperService;
    _tokenService: TokenService;
    utils = Utils
    BaseConsts = BaseConsts;
    ErrorMessage = ErrorMessage;
    SuccessMessage = SuccessMessage;
    YesNoConst = EYesNo;
    EActiveDeactive = EActiveDeactive;
    ActiveDeactiveConst = ActiveDeactiveConst;
    IconConfirm = EIconConfirm;

    constructor(
        injector: Injector,
    ) {
        this._libHelperService = injector.get(LibHelperService);
        this._tokenService = injector.get(TokenService);
    }

    screenHeight = window.innerHeight;
    screenWidth = window.innerWidth;
    isLoading: boolean = false;

    submitted: boolean = false;
    onClickSubmit: boolean = false;

    isValid: boolean = false;
    isSpinner: boolean = false;

    page: Page = new Page();

    activeIndex = 0;

    blockText: RegExp = /[0-9,.]/;
    number: RegExp = /[0-9]/;
    numberPercent: RegExp = /[0-9,]/;
    blockSpace: RegExp = /[^\s]/;
    regexTaxcode: RegExp = /[0-9-]/;

    destroyed$: ReplaySubject<boolean> = new ReplaySubject(3);

    todayDate = new Date();

    protected formatCalendarItemSendApi(datetime: Date) {
        if (!(datetime instanceof Date)) {
            return null
        }
        return moment(datetime, EFormatDate.DATE_YMD_Hms).format(EFormatDate.DATE_YMD_Hms);
    }

    public checkInValidForm(formGroup: FormGroup, skipValidationForFields: string[] = []): boolean {
        for (const i in formGroup.controls) {
            if (!skipValidationForFields.includes(i)) {
                formGroup.controls[i].markAsDirty();
            } else {
                formGroup.controls[i].clearValidators();
            }
            formGroup.controls[i].updateValueAndValidity({ emitEvent: false });
        }
        for (const key in formGroup.controls) {
            if (formGroup.controls[key].status === "INVALID") {
                Utils.log(`invalid`, { controlName: key, controls: formGroup.controls[key] });
            }
        }
        return formGroup.invalid;
    }

    isInvalidMessage(form: FormGroup, controlName: string, onClickSubmit?: boolean): boolean {
        return !!(form?.get(controlName)?.hasError('errorMessage') && form?.get(controlName)?.invalid && (form?.get(controlName)?.touched || onClickSubmit));
    }

    isInvalid(form: FormGroup, controlName: string, onClickSubmit?: boolean) {
        if (onClickSubmit) {
            return onClickSubmit && form?.get(controlName)?.invalid;
        }
        return !!(form?.get(controlName)?.invalid && form?.get(controlName)?.touched);
    }

    convertObjectToFormData(object: any, formData: FormData = new FormData(), parentKey: string | null = null, arrayIndex: number | null = null): FormData {
        for (const [key, value] of Object.entries(object)) {
            const finalKey = parentKey ? (arrayIndex !== null ? `${parentKey}[${arrayIndex}].${key}` : `${parentKey}.${key}`) : key;
            if (Array.isArray(value)) {
                for (let i = 0; i < value.length; i++) {
                    const arrayItemKey = `${finalKey}[${i}]`;
                    if (typeof value[i] === 'object' && value[i] !== null) {
                        this.convertObjectToFormData(value[i], formData, finalKey, i);
                    } else {
                        formData.append(arrayItemKey, String(value[i]));
                    }
                }
            } else if (typeof value === 'object' && value !== null) {
                if (value instanceof File) {
                    formData.append(finalKey, value, value.name);
                } else {
                    this.convertObjectToFormData(value, formData, finalKey);
                }
            } else {
                formData.append(finalKey, String(value));
            }
        }

        return formData;
    }

    handleResponseInterceptor(response: any, message?: string): boolean {
        return this._libHelperService.handleResponseInterceptor(response, message);
    }

    messageError(msg = '', summary = '', life = 3000) {
        return this._libHelperService.messageError(msg, summary, life);
    }

    messageSuccess(msg = '', summary = '', life = 1000) {
        return this._libHelperService.messageSuccess(msg, summary, life);
    }

    messageWarn(msg = '', life = 3000) {
        return this._libHelperService.messageWarn(msg, life);
    }

    checkValidForm(isValid: boolean) {
        if (!isValid) this.messageError('Vui lòng nhập đủ thông tin!');
        return isValid;
    }

    getUser() {
        let userInfo = Utils.getLocalStorage(BaseConsts.localStorageUser)
        return {
            role: userInfo?.role,
            email: userInfo?.email,
            username: userInfo?.username,
            id: userInfo?.id
        }
    }


    async confirmAction(message: string, icon?: EIconConfirm): Promise<boolean> {
        const responseConfirm = await this._libHelperService.awaitApi(this._libHelperService.dialogConfirmRef(
            message, {
            icon: icon || EIconConfirm.QUESTION
        }
        ).onClose);
        return !!responseConfirm;
    }


    markControls(formGroup: FormGroup, skipValidationForFields: string[] = []) {
        for (const i in formGroup.controls) {
            if (!skipValidationForFields.includes(i)) {
                formGroup.controls[i].markAsDirty();
            } else {
                formGroup.controls[i].clearValidators();
            }
            formGroup.controls[i].updateValueAndValidity({ emitEvent: false });
        }
    }

    checkStatusResponse(response, message?: string): boolean {
        return this._libHelperService.checkStatusResponse(response, message);
    }

    public inValidForm(formGroup: FormGroup, skipValidationForFields: string[] = []): boolean {
        this.markControls(formGroup, skipValidationForFields);

        Object.keys(formGroup.controls).forEach(key => {
            const control = formGroup.controls[key];
            if (control instanceof FormArray) {
                control.controls.forEach((controlItem: FormGroup | AbstractControl) => {
                    if (controlItem instanceof FormGroup) {
                        this.markControls(controlItem, skipValidationForFields);
                    }
                });
            }
        });

        let isInvalid = false;
        Object.keys(formGroup.controls).forEach(key => {
            if (formGroup.controls[key].status === "INVALID") {
                Utils.log(`invalid`, { controlName: key, controls: formGroup.controls[key] });
                isInvalid = true;
            }
        });
        if (isInvalid) {
            this.messageError(ErrorMessage.ERR_INVALID_FORM)
        }
        return isInvalid;
    }
}

