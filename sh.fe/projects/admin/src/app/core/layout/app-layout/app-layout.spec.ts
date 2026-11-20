import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { APP_MENU } from '@/core/layout/model/layout.models';
import { AppLayout } from './app-layout';

describe('AppLayout', () => {
    let fixture: ComponentFixture<AppLayout>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppLayout],
            providers: [
                provideRouter([]),
                provideHttpClient(),
                provideHttpClientTesting(),
                provideNoopAnimations(),
                providePrimeNG(),
                { provide: APP_MENU, useValue: [] },
                MessageService,
                ConfirmationService,
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AppLayout);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
