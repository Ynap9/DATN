import { EIconConfirm } from './../const/base.consts';


export interface IEConfirm {
    message: string,
    icon?: EIconConfirm,
    labelButton?: ILabelButton,
}

export interface IParamHandleDTO {
    idName?: string;
    isCheckNull?: boolean;
}

export interface IType {
    name?: string;
    code?: string | number;
}

export interface ITag extends IType {
    severity: string,
}

export interface IErrorCode {
    [key: number] : string,
}

export interface ILabelButton {
    accept: string;
    cancel: string;
}

export interface IDropdown{
    labelName: string,
    value: number | string
}

export interface IDialogConfirmConfig {
    header?: string,
    message?: string,
    width?: string,
    icon?: EIconConfirm,
    labelButton?: ILabelButton,
	labelAccept?: string,
	labelCancel?: string,
    isNoteConfirm?: boolean,
    dropdownData?: any[];
	styleClass?: string,
	maskStyleClass?: string,
    createBy?: string,
    createDate?: string
}

export interface IConfirmNavigation{
    active?: boolean,
    message?: string
}

export interface IRadioButtonValue {
    label: string,
    value: any,
}

export interface IEventSubmitForm {
    hasChangeData: boolean,
	originalData: any,
	actionType: 'create' | 'update' | 'confirm'
}
