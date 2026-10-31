import { Component, input } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { BreadcrumbModule } from 'primeng/breadcrumb';

@Component({
    selector: 'app-breadcrumb',
    imports: [BreadcrumbModule],
    templateUrl: './breadcrumb.component.html',
    styleUrl: './breadcrumb.component.scss',
})
export class Breadcrumb {
    items = input.required<MenuItem[]>();
    home = input.required<MenuItem>();
}
