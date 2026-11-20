import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_MENU } from '@/core/layout/model/layout.models';
import { AppSidebar } from './app-sidebar';

describe('AppSidebar', () => {
    let fixture: ComponentFixture<AppSidebar>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppSidebar],
            providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), { provide: APP_MENU, useValue: [] }],
        }).compileComponents();

        fixture = TestBed.createComponent(AppSidebar);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
