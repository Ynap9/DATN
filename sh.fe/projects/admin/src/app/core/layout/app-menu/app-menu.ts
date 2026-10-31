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
            items: group.items?.filter((item) => !item['permission'] || this._sharedService.isGranted(item['permission'])),
        }))
        .filter((group) => group.items?.length);
}
