import { Component, inject, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { LayoutService } from '../service/layout.service';
import { CommonModule } from '@angular/common';
import { ShBreadcrumbComponent } from '@sh/ui';
import { SharedService } from '@/services/shared.service';

@Component({
    selector: 'app-topbar',
    imports: [CommonModule, ShBreadcrumbComponent],
    templateUrl: './app-topbar.html',
    styleUrl: './app-topbar.scss',
})
export class AppTopbar implements OnInit {

    items!: MenuItem[];

    layoutService = inject(LayoutService);
    private readonly _sharedService = inject(SharedService);

    today = '';

    get displayName(): string {
        const user = this._sharedService.currentUser;
        return user?.fullName || user?.userName || '';
    }

    ngOnInit(): void {
        this.today = new Date().toLocaleDateString('vi-VN', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
        });
    }
}
