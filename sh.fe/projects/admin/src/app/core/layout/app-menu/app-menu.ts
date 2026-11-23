import { Component, inject } from '@angular/core';
import { SharedService } from '@/services/shared.service';
import { APP_MENU } from '../model/layout.models';
import { AppMenuitem } from '../app-menuitem/app-menuitem';

@Component({
    selector: 'app-menu',
    imports: [AppMenuitem],
    templateUrl: './app-menu.html',
    styleUrl: './app-menu.scss',
})
export class AppMenu {
    private readonly _sharedService = inject(SharedService);

    model = inject(APP_MENU)
        .map((group) => ({
            ...group,
            items: group.items?.filter((item) => this.canView(item['permission'])),
        }))
        .filter((group) => group.items?.length);

    // permission là một key hoặc mảng key: có một trong số đó là hiện mục menu
    private canView(permission?: string | string[]) {
        if (!permission) return true;
        return (Array.isArray(permission) ? permission : [permission]).some((key) => this._sharedService.isGranted(key));
    }
}
