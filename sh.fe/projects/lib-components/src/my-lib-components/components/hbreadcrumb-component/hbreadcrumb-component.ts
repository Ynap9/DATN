import { Component, Input } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Subject, Subscription } from 'rxjs';
import { HBreadcrumService } from './hbreadcrum.service';

@Component({
    selector: 'h-breadcrumb-component',
    standalone: false,
    templateUrl: './hbreadcrumb-component.html',
    styleUrl: './hbreadcrumb-component.scss',
})
export class HbreadcrumbComponent {

    subscription: Subscription;
    items: MenuItem[];

    constructor(public breadcrumbService: HBreadcrumService) {
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
