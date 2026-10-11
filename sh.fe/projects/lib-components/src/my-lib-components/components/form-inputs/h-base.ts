import { ChangeDetectorRef, Directive, DoCheck, InjectionToken, Input, NgZone, OnChanges, OnDestroy, OnInit, Optional, Self, SimpleChanges } from "@angular/core";
import { AbstractControl, ControlValueAccessor, NgControl } from "@angular/forms";
import { v4 as uuidv4 } from 'uuid';

export const H_NG_VALUE_ACCESSOR = new InjectionToken<ReadonlyArray<ControlValueAccessor>>('LibNgValueAccessor');

@Directive()
export abstract class HBaseControl implements OnInit, DoCheck, OnDestroy, ControlValueAccessor {

	constructor(
        @Optional() @Self() public controlDir: NgControl,
        public cd: ChangeDetectorRef,
        public ngZone: NgZone,
    ) {
        if (controlDir) {
            controlDir.valueAccessor = this;
        }
    }

	@Input() label: string = '';
	@Input() pTooltipLabel: string = '';
    @Input() index: number;
	@Input() inputId: string;
	@Input() placeholder: string;
	@Input() isShowMessageInvalid: boolean = true;
	@Input() name: string;
	@Input() required: boolean = false;
	@Input() disableWhenRemoved?: boolean = false;
	@Input() disableAndResetValueWhenRemoved?: boolean = false;

	@Input() value: any;
	@Input() disabled: boolean;
	@Input() dynamicRequired: any;

    isRequired: boolean;

	get control() {
		return this.controlDir?.control;
	}

	changeDetectionCount: number = 0;

	_onChange = (value: any) => { };
	onTouched = () => { };

	ngOnInit(): void {
		this.setPlaceHolder();
		this.onInit();
	}

	ngOnChanges(changes: SimpleChanges) {
		if(changes['required']) {
			this.isRequired = changes['required']?.currentValue;
		}
	}

	onInit() {
		const index = (this.index || this.index === 0) ? `-${this.index}` : '';
        this.inputId = (this.inputId || this.controlDir?.name || uuidv4()) + index;
		this.setValidators();
	}

	setPlaceHolder() {
		this.placeholder = this.placeholder === undefined ? this.label : this.placeholder;
	}

	writeValue(value: any): void {
        this.value = value;
		this.cd.markForCheck();
    }

	registerOnChange(fn: any): void {
	  	this._onChange = fn;
	}

	registerOnTouched(fn: any): void {
	  	this.onTouched = fn;
	}

	isDisabled: boolean = false;
    setDisabledState(disabled: boolean): void {
        this.isDisabled = disabled;
		this.cd.markForCheck();
    }

	validators: any;
	setValidators() {
		this.validators = this.control?.validator ? [this.control.validator] : [];
		this.setRequired();
		this.control?.setValidators(this.validators);
		this.control?.updateValueAndValidity();
	}

	setRequired() {
		const validators = this.control?.validator;
		if(validators) {
			const validatorFn = validators({} as AbstractControl);
			this.isRequired = !!(validators && validatorFn?.['required']);
		} else {
			this.isRequired = false;
		}
		this.cd.markForCheck();
	}

	compareValidator(validator1, validator2) {
		return validator1.toString() === validator2.toString();
	}

	pristineStatus: boolean;
	compareValidatorStatusBefore: boolean;
	updateValidatorComplete: boolean;
	timeoutUpdateValidator: any
	ngDoCheck() {
		if(this.dynamicRequired) {
			const validators = this.control?.validator ? [this.control?.validator] : [];
			const updateValidator = !this.compareValidator(this.validators, validators);

			if(updateValidator && !this.updateValidatorComplete) {
				this.updateValidatorComplete = true;
				this.setValidators();
				if(this.timeoutUpdateValidator) clearTimeout(this.timeoutUpdateValidator)
				this.timeoutUpdateValidator = setTimeout(() => {
					this.updateValidatorComplete = false;
				}, 300);
			}
		}
	}

	ngOnDestroy(): void {
		if(this.disableWhenRemoved) {
		}
	}
}
