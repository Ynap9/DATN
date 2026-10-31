import { Component, inject, OnInit } from '@angular/core';

import { AuthConstants } from '@/shared/constants/auth.constants';
import { AuthService } from '@/core/auth/service/auth.service';

@Component({
    selector: 'app-callback',
    standalone: true,
    template: ``
})
export class AuthCallback implements OnInit {

    _authService = inject(AuthService);

    ngOnInit(): void {
        const query = window.location.search.substring(1); // ?code=...&state=...
        this.handleAuthCallback(query);
    }

    handleAuthCallback(query: string) {
        const params = new URLSearchParams(query);
        const code = params.get('code');

        if (!code) {
            console.error('No authorization code found');
            return;
        }

        const verifier = sessionStorage.getItem(AuthConstants.SESSION_PKCE_CODE_VERIFIER);

        this._authService.postConnectAuthorize(code, verifier ?? "");
    }
}
