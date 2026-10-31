import { ErrorMessage, EFormatDateDisplay } from '../shared/const/base.consts';
import { Utils } from '../utils';
import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import moment from 'moment';
export const messageRequired = 'Trường bắt buộc nhập';

export interface IValidRangeNumber {
    min?: number,
    max?: number,
    minMessage?: string,
    maxMessage?: string,
    field?: string,
    unit?: string
}

export function numberValidator(isRequired?: boolean): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const number = /^\d+$/;
		if (isRequired && ((control?.value && !control?.value.toString().trim()) || !control?.value)) {
			return { errorMessage: messageRequired };
		}
		if (!number.test(control?.value)) {
			return { errorMessage: 'Giá trị phải là số' };
		}

		return null;
	};
}

export function upperCaseValidator(): ValidatorFn {
	const fullNamePattern: RegExp = /^[^\p{Ll}]*$/u;

	return (control: AbstractControl): { [key: string]: any } | null => {
	  if (!control.value || !control.value.trim()) {
		return { errorMessage: messageRequired };
	  }

	  if (!fullNamePattern.test(control.value)) {
		return { errorMessage: 'Vui lòng nhập kí tự viết hoa' };
	  }

	  return null;
	};
}

export function bankAccountNameValidator(): ValidatorFn {
	const bankNamePattern: RegExp = /^[A-Z0-9\s!"#$%&'()*+,\-./:;<=>?@[\]^_`{|}~\\]+$/

	return (control: AbstractControl): { [key: string]: any } | null => {
		if ((control.value && !control.value.toString().trim()) || !control.value) {
			return {  errorMessage: messageRequired };
		}

		if (!bankNamePattern.test(control.value)) {
			return { errorMessage: 'Vui lòng nhập kí tự viết hoa không dấu' };
		  }

		  return null;
	};
}

export function blockNumberValidator(): ValidatorFn {
    const numberPattern: RegExp = /\d/;

    return (control: AbstractControl): { [key: string]: any } | null => {
        if (control.value && numberPattern.test(control.value)) {
            return { errorMessage: 'Vui lòng không nhập số' };
        }
        return null;
    };
}

export function arrayRequiredValidator(): ValidatorFn {
	return (control: AbstractControl): ValidationErrors | null => {
	  if (control.value && Array.isArray(control.value) && control.value.length > 0) {
		return null;
	  } else {
		return { errorMessage: ErrorMessage.ERR_ARRAY_INVALID };
	  }
	};
  }

export function required(isRequired: boolean = true, maxLength?: number): ValidatorFn {
    const required = (control: AbstractControl): { [key: string]: any } | null => {
        if (isRequired) {
            if ((['number', 'string'].includes(typeof control?.value) && !control.value.toString().trim()) || (!control?.value && control?.value !== 0) ) {
                return { errorMessage: messageRequired, required: true };
            }
        }
        if (maxLength && control.value && control.value.toString().length > maxLength) {
            return { errorMessage: `Độ dài tối đa là ${maxLength}` };
        }
        return null;
    };
	return required;
}

export function requiredWithCondition(fieldName: string, value: any): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		if (control?.parent?.get(fieldName)?.value === value) {
			if ((['number', 'string', 'boolean'].includes(typeof control?.value) && !control.value.toString().trim()) || (!control?.value && control?.value !== 0)) {
				return { errorMessage: messageRequired };
			}
		}
		return null;
	};
}


export function requiredArray(messageError?: string): ValidatorFn {
    const requiredArray = (control: AbstractControl): { [key: string]: string | boolean } | null => {
		if ((Array.isArray(control?.value) && control?.value?.length === 0) || !control?.value) {
			return { errorMessage: (messageError || 'Vui lòng chọn ít nhất 1 phần tử!'), required: true };
		}
        return null;
    };
	return requiredArray
}

export function cancelValidator(): ValidatorFn {
    return (control: AbstractControl): { [key: string]: string } | null => {
        return null;
    };
}

export function positiveNumber(message?:string): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } | null => {
        if (control.value === 0) {
            return { errorMessage: (message || 'Chỉ nhận giá trị lớn hơn 0') };
        }
        return null;
    };
}

export function rangeNumberValidator(params: IValidRangeNumber): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		if (params?.min && control.value && control.value < params?.min) {
            return { errorMessage: params?.minMessage || `${params?.field} chưa đạt tối thiểu là ${Utils.formatCurrency(params?.min)} ${params?.unit || ''}` };
        } else if (params?.max && control.value && control.value > params?.max) {
            return { errorMessage: params?.maxMessage || `${params?.field} vượt quá tối đa là ${Utils.formatCurrency(params?.max)} ${params?.unit || ''}` };
        }
        return null;
    };
}

export function emailValidator(isRequired: boolean = true): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
    if (isRequired && !control.value) {
      return { errorMessage: messageRequired };
    }

    if (control.value && !emailPattern.test(control.value)) {
      return { errorMessage: ErrorMessage.ERR_EMAIL_INVALID };
    }
    return null;
  };
}

export function phoneValidator(isRequired: boolean = true): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
	const phoneNumber = Utils.convertToVietNamesePhone(control?.value)
    const phonePattern = /^(032|033|034|035|036|037|038|039|096|097|098|086|088|091|094|081|082|083|084|085|090|093|089|070|079|077|076|078|052|056|058|092|087|055|059|099)\d{7}$/;

	if (isRequired && !phoneNumber) {
      return { errorMessage: messageRequired };
    }

    if (phoneNumber && !phonePattern.test(phoneNumber)) {
      return { errorMessage: ErrorMessage.ERR_PHONE_INVALID };
    } else if(phoneNumber && control.value !== phoneNumber) {
		control.setValue(Utils.convertToVietNamesePhone(phoneNumber))
	}

    return null;
  };
}

export function enumValidator(enumType: any): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const value = control.value;

		if (Object.values(enumType).includes(value)) {
			return null;
		} else {
			return { errorMessage: 'Giá trị không hợp lệ' };
		}
	};
}

export function dateRangeValidator(minDate: Date | null, maxDate: Date | null, isRequired: boolean = true): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		if (isRequired && Validators.required(control)) {
			return { errorMessage: messageRequired };
		}

		const selectedDate = new Date(control.value);

		if (isNaN(selectedDate.getTime())) {
			return { errorMessage: 'Giá trị không hợp lệ' };
		}

		if (minDate && selectedDate < minDate) {
			return { errorMessage: `Giá trị phải hơn ${formatCalendar(minDate)}` };
		}

		if (maxDate && selectedDate > maxDate) {
			return { errorMessage: `Giá trị nhỏ hơn ${formatCalendar(maxDate)}` };
		}

		return null;
	};
}

export function formatCalendar(datetime: Date) {
	if (!(datetime instanceof Date)) {
		return null;
	}
	return moment(datetime, EFormatDateDisplay.DATE_DMY).format(
		EFormatDateDisplay.DATE_DMY
	);
}

export function passwordValidator(isRequired: boolean = true): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		if (isRequired) {
			if (!control?.value) {
				return { errorMessage: messageRequired};
			}
			if (control?.value?.length < 8) {
				return { errorMessage: 'Mật khẩu chứa ít nhất 8 ký tự' };
			  }
			if (!/[A-Z]/.test(control?.value)) {
				return { errorMessage: 'Mật khẩu phải có ít nhất một chữ viết hoa' };
			}

			if (!/[\W_]/.test(control?.value)) {
				return { errorMessage: 'Mật khẩu phải có ít nhất một ký tự đặc biệt' };
			}

			if (!/\d/.test(control?.value)) {
				return { errorMessage: 'Mật khẩu phải có ít nhất một số' };
			}
		}
	  	return null;
	};
}

export function usernameValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const usernamePattern = /^[a-zA-Z0-9_-]{3,30}$/;
		if (control.value && !usernamePattern.test(control.value)) {
		  return { errorMessage: "Tên đăng nhập viết liền không dấu tối thiểu 3 ký tự bao gồm các ký tự không dấu a-z, chữ số 0-9, dấu '-', dấu '_'"};
		}

		return null;
	};
}

export function emojiValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const value = control.value;
		const emojiPattern = /[\u{1F600}-\u{1F64F}\u{1F300}-\u{1F5FF}\u{1F680}-\u{1F6FF}\u{1F700}-\u{1F77F}\u{1F780}-\u{1F7FF}\u{1F800}-\u{1F8FF}\u{1F900}-\u{1F9FF}\u{1FA00}-\u{1FA6F}\u{2600}-\u{26FF}\u{2700}-\u{27BF}\u{2B50}\u{2B06}]+/u;
		if (emojiPattern.test(value)) {
			return { errorMessage: ErrorMessage.ERR_EMOJI_INVALID};
		}
		return null;
	};

}
export function nameBusinessValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const nameBusinessPattern = /^[\p{L}0-9,.&+\-_%():/;%"'\\ ]+$/u;
		if (control.value && !nameBusinessPattern.test(control.value)) {
			return { errorMessage: ErrorMessage.ERR_NAME_BUSINESS_INVALID };
		}

		return null;
	};
}

export function conditionPassword(): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } | null => {
        const password = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d!@#$&*-_?%()]{8,}$/;
        if (control.value && !password.test(control.value)) {
          return { errorMessage: "Mật khẩu tối thiểu 8 ký tự có ít nhất 1 chữ viết hoa, chữ thường, số "};
        }

        return null;
    };
}

export function mustMatch(controlName: string, matchingControlName: string) {
    return (formGroup: FormGroup) => {
        const control = formGroup.controls[controlName];
        const matchingControl = formGroup.controls[matchingControlName];

        if (control.value !== matchingControl.value) {
            matchingControl.setErrors({ confirmedValidator: true });
        } else {
            matchingControl.setErrors(null);
        }
        return null;
    }
}

export function justAlphabetAndNumberValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const alphabetAndNumberPattern = /^[a-zA-Z0-9\s]+$/u;
		if (control.value && !alphabetAndNumberPattern.test(control.value)) {
			return { errorMessage: ErrorMessage.ERR_JUST_ALPHABET_AND_NUM_INVALID };
		}

		return null;
	};
}

export function longitudeAndLatitudeValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const longitudeAndLatitudePattern = /^[1-9][0-9]*(\.[0-9]+)?$/u;
		if (control.value && !longitudeAndLatitudePattern.test(control.value)) {
			return { errorMessage: ErrorMessage.ERR_LONGITUDE_LATITUDE_INVALID };
		}

		return null;
	};
}
export function decimalValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const number = /^\d+(\,\d{1,2})?$/;
		if ((control.value && !control.value.toString().trim()) || !control.value) {
			return { errorMessage: messageRequired };
		}
		if (!number.test(control.value)) {
			return { errorMessage: 'Giá trị không hợp lệ' };
		}

		return null;
	};
}

export function ipValidator(): ValidatorFn {
	return (control: AbstractControl): { [key: string]: any } | null => {
		const ipPattern = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
		if ((control.value && !control.value.toString().trim()) || !control.value) {
			return { errorMessage: messageRequired };
		}
		if (!ipPattern.test(control.value)) {
			return { errorMessage: 'Giá trị không hợp lệ' }
		}
		return null;
	}
}
