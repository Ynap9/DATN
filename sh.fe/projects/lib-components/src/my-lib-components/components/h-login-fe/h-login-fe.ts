import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
    selector: 'h-login-fe',
    standalone: false,
    templateUrl: './h-login-fe.html',
    styleUrl: './h-login-fe.scss',
})
export class HLoginFeComponent {
    constructor(
        private fb: FormBuilder,
        private cd: ChangeDetectorRef,
    ) {
    }

    postForm: FormGroup;

    messageError: string;
    submitting = false;

    ngOnInit() {
        this.formInit();
    }

    formInit() {
        this.postForm = this.fb.group({
            username: ['', Validators.required],
            password: ['', Validators.required]
        })
    }

    onSubmit() {
        if (this.submitting) return;
        this.messageError = '';

        if (this.postForm.invalid) {
            this.postForm.markAllAsTouched();
            this.messageError = 'Vui lòng nhập tên đăng nhập và mật khẩu.';
            this.cd.markForCheck();
            return;
        }
    }

    focusPassword() {
        const UserField = document.getElementById('password');
        if (UserField) {
            UserField.focus();
        }
    }

    focusLogin() {
        const loginField = document.getElementById('login');
        if (loginField) {
            loginField.focus();
        }
    }
}
