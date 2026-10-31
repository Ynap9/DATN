import { SKIP_AUTH, SKIP_ERROR_TOAST } from "@/core/http/http-context";
import { SharedService } from "@/services/shared.service";
import { AuthConstants } from "@/shared/constants/auth.constants";
import { HttpClient, HttpContext, HttpParams, HttpHeaders } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { environment } from "@env/environment";
import { Utils } from "@sh/ui";
import { concatMap, finalize, Observable, of, shareReplay } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
    http = inject(HttpClient);
    router = inject(Router);
    private readonly _sharedService = inject(SharedService);
    baseUrl = environment.baseUrl;
    private refreshInFlight$: Observable<unknown> | null = null;

    login(username: string, password: string) {
        const body = new HttpParams()
            .set('username', username)
            .set('password', password)
            .set('grant_type', environment.authGrantType)
            .set('client_id', environment.authClientId)
            .set('scope', environment.authScope);
        const headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded',
            Accept: 'text/plain'
        });

        const context = new HttpContext().set(SKIP_AUTH, true).set(SKIP_ERROR_TOAST, true);

        return this.http.post(`${this.baseUrl}/connect/token`, body.toString(), { headers, context, withCredentials: true }).pipe(
            concatMap((res: any) => {
                const redirect_uri = Utils.getSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN) || '/';
                // Utils.removeSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN)

                this.router.navigate([redirect_uri]);

                return of(res);
            })
        );
    }

    refreshAccessToken() {
        const body = new HttpParams()
            .set('grant_type', 'refresh_token')
            .set('client_id', environment.authClientId);
        const headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded',
            Accept: 'text/plain'
        });
        const context = new HttpContext().set(SKIP_AUTH, true).set(SKIP_ERROR_TOAST, true);

        this.refreshInFlight$ ??= this.http.post(`${this.baseUrl}/connect/token`, body.toString(), { headers, context, withCredentials: true }).pipe(
            finalize(() => (this.refreshInFlight$ = null)),
            shareReplay(1),
        );

        return this.refreshInFlight$;
    }

    postConnectAuthorize(code: string, code_verifier: string) {
        // Exchange code for tokens
        const body = new HttpParams()
            .set('code', code)
            .set('code_verifier', code_verifier)
            .set('grant_type', 'authorization_code')
            .set('client_id', environment.authClientId)
            .set('redirect_uri', `${environment.appUrl}/auth/callback`);

        const headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded',
            Accept: 'text/plain'
        });

        return this.http.post<any>(`${environment.baseUrl}/connect/token`, body.toString(), { headers, withCredentials: true }).subscribe({
            next: () => {
                Utils.removeSessionStorage(AuthConstants.SESSION_PKCE_CODE_VERIFIER);
                Utils.removeSessionStorage(AuthConstants.PKCE_CODE_CHALLENGE_METHOD);

                const redirect_uri = Utils.getSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN) || '/';
                // Utils.removeSessionStorage(AuthConstants.REDIRECT_URI_AFTER_LOGIN)

                this.router.navigateByUrl(redirect_uri);
            },
            error: (err) => console.error('Token exchange failed', err)
        });
    }

    logout() {
        const context = new HttpContext().set(SKIP_AUTH, true).set(SKIP_ERROR_TOAST, true);
        this.http.post(`${this.baseUrl}/connect/logout`, null, { context, withCredentials: true }).subscribe({ error: () => {} });

        Utils.clearLocalStorage();
        Utils.clearSessionStorage();
        this._sharedService.clearAll();
        this.router.navigate(['auth/login']);
    }
}