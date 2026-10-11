import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ButtonModule } from 'primeng/button';
import { StyleClassModule } from 'primeng/styleclass';
import { RippleModule } from 'primeng/ripple';
import { RouterModule } from '@angular/router';
import { MenuModule } from 'primeng/menu';

import { AppConfigurator } from './layout/app-configurator/app-configurator';
import { AppFloatingConfigurator } from './layout/app-floating-configurator/app-floating-configurator';
import { AppFooter } from './layout/app-footer/app-footer';
import { AppLayout } from './layout/app-layout/app-layout';
import { AppMenu } from './layout/app-menu/app-menu';
import { AppMenuitem } from './layout/app-menuitem/app-menuitem';
import { AppSidebar } from './layout/app-sidebar/app-sidebar';
import { AppTopbar } from './layout/app-topbar/app-topbar';
import { MyLibComponentsModule } from '../../../../lib-components/src/public-api';

@NgModule({
    declarations: [
        AppConfigurator,
        AppFloatingConfigurator,
        AppFooter,
        AppLayout,
        AppMenu,
        AppMenuitem,
        AppSidebar,
        AppTopbar,
    ],
    imports: [
        CommonModule,
        FormsModule,
        SelectButtonModule,
        ButtonModule,
        StyleClassModule,
        RippleModule,
        RouterModule,
        MenuModule,
        MyLibComponentsModule
    ],
    exports: [
        AppConfigurator,
        AppFloatingConfigurator,
        AppFooter,
        AppLayout,
        AppMenu,
        AppMenuitem,
        AppSidebar,
        AppTopbar,
    ],
})
export class CoreModule { }
