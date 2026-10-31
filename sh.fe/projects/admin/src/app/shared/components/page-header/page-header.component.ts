import { Component, input } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Breadcrumb } from '../breadcrumb/breadcrumb.component';

@Component({
    selector: 'app-page-header',
    imports: [Breadcrumb],
    templateUrl: './page-header.component.html',
    styleUrl: './page-header.component.scss',
})
export class PageHeader {
    title = input.required<string>();
    breadcrumbs = input.required<MenuItem[]>();

    readonly home: MenuItem = { icon: 'pi pi-home', routerLink: '/admin' };
}
