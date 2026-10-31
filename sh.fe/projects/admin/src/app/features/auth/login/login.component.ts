import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { ToastModule } from 'primeng/toast';
import { ShInputTextComponent, Utils } from '@sh/ui';
import { SharedService } from '@/services/shared.service';
import { SharedImports } from '@/shared/import.shared';
import { AuthService } from '@/core/auth/service/auth.service';
import { BaseComponent } from '@/shared/base/base.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { environment } from '@env/environment';

@Component({
    selector: 'app-login',
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, PasswordModule, ToastModule, ShInputTextComponent,SharedImports],
    templateUrl: './login.component.html',
    styleUrl: './login.component.scss',
})
export class LoginComponent extends BaseComponent  {
    private readonly _fb = inject(FormBuilder);
    private readonly _cd = inject(ChangeDetectorRef);
    private readonly _authService = inject(AuthService);

    postForm: FormGroup;
    override form = new FormGroup({
        username: new FormControl('', [Validators.required]),
        password: new FormControl('', [Validators.required])
    });
    submitting = false;
    override loading: boolean = false;
    redirectUri: string | null | undefined = ''

    override ValidationMessages: Record<string, Record<string, string>> = {
        username: {
            required: 'Không được bỏ trống'
        },
        password: {
            required: 'Không được bỏ trống'
        }
    };

    override ngOnInit(): void {
        this._activatedRoute.queryParamMap.subscribe((params) => {
            this.redirectUri = params.get('redirect_uri') || '/';
            Utils.setSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN, this.redirectUri)
        });
    }

    onSubmit() {
        if (this.isFormInvalid()) return;

        this.loading = true;

        this._authService
            .login(this.form.value.username!, this.form.value.password!)
            .subscribe({
                next: (res) => {
                    
                },
                error: (resErr) => {
                    this._messageService.add({
                        severity: 'error',
                        detail: resErr?.error?.error_description || 'Có sự cố xảy ra. Vui lòng thử lại sau',
                        life: 4000
                    });
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    async redirectBackendAuthorize() {
        const backendUrl = environment.baseUrl;
        const clientId = environment.authClientId;
        const redirectUri = 'http://localhost:4200/auth/callback';
        const { codeChallenge, codeVerifier } = await Utils.generatePKCECodes();

        window.location.href = `${backendUrl}/login/google`;
    }

    async loginMs() {
        const backendUrl = environment.baseUrl;
        const redirectUri = `${environment.appUrl}/auth/callback`;
        const { codeChallenge, codeVerifier } = await Utils.generatePKCECodes();

        sessionStorage.setItem(AuthConstants.SESSION_PKCE_CODE_VERIFIER, codeVerifier);

        const url =
            `${backendUrl}/connect/authorize?` +
            `client_id=${encodeURIComponent(environment.authClientId)}` +
            `&redirect_uri=${encodeURIComponent(redirectUri)}` +
            `&response_type=code` +
            `&scope=openid offline_access` +
            `&prompt=login&code_challenge=${codeChallenge}&code_challenge_method=${AuthConstants.PKCE_CODE_CHALLENGE_METHOD}`;

        window.location.href = url;
    }



    focusPassword(): void {
        document.getElementById('password')?.focus();
    }

    focusLogin(): void {
        document.getElementById('login')?.focus();
    }
}
