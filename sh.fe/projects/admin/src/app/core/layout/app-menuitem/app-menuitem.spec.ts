import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppMenuitem } from './app-menuitem';

describe('AppMenuitem', () => {
    let fixture: ComponentFixture<AppMenuitem>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppMenuitem],
            providers: [provideRouter([])],
        }).compileComponents();

        fixture = TestBed.createComponent(AppMenuitem);
        fixture.componentRef.setInput('item', { label: 'Dự án', routerLink: ['/admin/construction/project'] });
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(fixture.componentInstance).toBeTruthy();
    });
});
