import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APP_MENU } from '@/core/layout/model/layout.models';
import { AppMenu } from './app-menu';

describe('AppMenu', () => {
    let fixture: ComponentFixture<AppMenu>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppMenu],
            providers: [provideRouter([]), { provide: APP_MENU, useValue: [] }],
        }).compileComponents();

        fixture = TestBed.createComponent(AppMenu);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
