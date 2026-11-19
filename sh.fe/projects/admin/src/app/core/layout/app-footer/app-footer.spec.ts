import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppFooter } from './app-footer';

describe('AppFooter', () => {
    let fixture: ComponentFixture<AppFooter>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppFooter],
        }).compileComponents();

        fixture = TestBed.createComponent(AppFooter);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
