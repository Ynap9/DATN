import { Component, Input } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Subject, Subscription } from 'rxjs';
import { ShBreadcrumbService } from './sh-breadcrumb.service';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'sh-breadcrumb',
    imports: [CommonModule, RouterLink],
    templateUrl: './sh-breadcrumb.html',
    styleUrl: './sh-breadcrumb.scss',
})
export class ShBreadcrumbComponent {

    subscription: Subscription;
    items: MenuItem[];

    constructor(public breadcrumbService: ShBreadcrumbService) {
        this.subscription = breadcrumbService.itemsHandler.subscribe(response => {
            this.items = response;
        });
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}
