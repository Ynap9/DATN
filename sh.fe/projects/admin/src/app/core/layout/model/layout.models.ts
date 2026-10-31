import { InjectionToken } from '@angular/core';
import { MenuItem } from 'primeng/api';

export const APP_MENU = new InjectionToken<MenuItem[]>('APP_MENU');

export interface LayoutConfig {
    menuMode: string;
}

export interface LayoutState {
    staticMenuDesktopInactive: boolean;
    overlayMenuActive: boolean;
    mobileMenuActive: boolean;
    menuHoverActive: boolean;
    activePath: string | null;
}


