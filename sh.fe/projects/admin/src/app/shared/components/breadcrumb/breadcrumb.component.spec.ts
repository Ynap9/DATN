import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { Breadcrumb } from './breadcrumb.component';

describe('Breadcrumb', () => {
    it('hiện home và các mục', () => {
        TestBed.configureTestingModule({ providers: [provideRouter([]), providePrimeNG()] });
        const fixture = TestBed.createComponent(Breadcrumb);
        fixture.componentRef.setInput('home', { icon: 'pi pi-home', routerLink: '/admin' });
        fixture.componentRef.setInput('items', [{ label: 'Vai trò' }]);
        fixture.detectChanges();

        const element: HTMLElement = fixture.nativeElement;
        expect(element.querySelector('.pi-home')).toBeTruthy();
        expect(element.textContent).toContain('Vai trò');
    });
});
