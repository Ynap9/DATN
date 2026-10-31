import { Component, computed, effect, inject } from '@angular/core';
import { LayoutService } from '../service/layout.service';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { ShBreadcrumbComponent } from '@sh/ui';
import { AppTopbar } from '../app-topbar/app-topbar';
import { AppSidebar } from '../app-sidebar/app-sidebar';

@Component({
    selector: 'app-layout',
    imports: [CommonModule, RouterOutlet, ToastModule, ConfirmDialog, ShBreadcrumbComponent, AppTopbar, AppSidebar],
    templateUrl: './app-layout.html',
    styleUrl: './app-layout.scss',
})
export class AppLayout {
    layoutService = inject(LayoutService);

    constructor() {
        effect(() => {
            const state = this.layoutService.layoutState();
            if (state.mobileMenuActive) {
                document.body.classList.add('blocked-scroll');
            } else {
                document.body.classList.remove('blocked-scroll');
            }
        });
    }

    containerClass = computed(() => {
        const config = this.layoutService.layoutConfig();
        const state = this.layoutService.layoutState();
        return {
            'layout-overlay': config.menuMode === 'overlay',
            'layout-static': config.menuMode === 'static',
            'layout-static-inactive': state.staticMenuDesktopInactive && config.menuMode === 'static',
            'layout-overlay-active': state.overlayMenuActive,
            'layout-mobile-active': state.mobileMenuActive
        };
    })
}
