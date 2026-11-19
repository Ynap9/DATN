import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppTopbar } from './app-topbar';

describe('AppTopbar', () => {
    let fixture: ComponentFixture<AppTopbar>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppTopbar],
            providers: [provideRouter([])],
        }).compileComponents();

        fixture = TestBed.createComponent(AppTopbar);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
