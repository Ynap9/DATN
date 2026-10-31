import { HttpClient, HttpContext, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SKIP_AUTH } from './http-context';

@Injectable({ providedIn: 'root' })
export class DownloadService {
    private readonly _http = inject(HttpClient);

    get(url: string, skipAuth = false): Observable<HttpResponse<Blob>> {
        return this._http.get(url, { responseType: 'blob', observe: 'response', context: this.context(skipAuth) });
    }

    post(url: string, body: unknown, skipAuth = false): Observable<HttpResponse<Blob>> {
        return this._http.post(url, body, { responseType: 'blob', observe: 'response', context: this.context(skipAuth) });
    }

    private context(skipAuth: boolean): HttpContext {
        return new HttpContext().set(SKIP_AUTH, skipAuth);
    }
}
