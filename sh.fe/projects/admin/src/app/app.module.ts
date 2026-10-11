import { MyLibComponentsModule } from './../../../lib-components/src/my-lib-components/my-lib-components.module';
import { GlobalErrorHandler } from './../../../lib-components/src/my-lib-components/shared/services/auth/global-error-handler.service';
import { HandleInterceptorService } from './../../../lib-components/src/my-lib-components/shared/services/auth/handle.interceptor.service';
import { ErrorHandler, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

import { App } from './app';
import { CoreModule } from './core/core.module';
import { AppRoutingModule } from './app-routing.module';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { CommonModule } from '@angular/common';

@NgModule({
    declarations: [
        App
    ],

    imports: [
        BrowserModule,
        BrowserAnimationsModule,
        HttpClientModule,
        RouterModule,
        AppRoutingModule,
        CommonModule,
        CoreModule,
        MyLibComponentsModule.forRoot()
    ],
    providers: [
        providePrimeNG({
            theme: {
                preset: Aura,
                options: {
                    darkModeSelector: false
                }
            }
        })
        ,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: HandleInterceptorService,
            multi: true,
        },
        { provide: ErrorHandler, useClass: GlobalErrorHandler },
        MessageService,
        DialogService,
        ConfirmationService,
    ],
    bootstrap: [App]
})
export class AppModule { }
