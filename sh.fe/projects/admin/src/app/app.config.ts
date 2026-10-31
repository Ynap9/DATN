import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import Aura from '@primeuix/themes/aura';
import { GlobalErrorHandler } from '@/core/error/global-error-handler';
import { authInterceptor } from '@/core/http/auth.interceptor';
import { baseUrlInterceptor } from '@/core/http/base-url.interceptor';
import { httpErrorInterceptor } from '@/core/http/http-error.interceptor';
import { APP_MENU } from '@/core/layout/model/layout.models';
import { APP_MENU_ITEMS } from './app.menu';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes),
        provideHttpClient(withInterceptors([baseUrlInterceptor, httpErrorInterceptor, authInterceptor])),
        provideAnimationsAsync(),
        providePrimeNG({
            theme: {
                preset: Aura,
                options: {
                    darkModeSelector: false
                }
            }
        }),
        { provide: ErrorHandler, useClass: GlobalErrorHandler },
        { provide: APP_MENU, useValue: APP_MENU_ITEMS },
        MessageService,
        DialogService,
        ConfirmationService,
    ],
};
