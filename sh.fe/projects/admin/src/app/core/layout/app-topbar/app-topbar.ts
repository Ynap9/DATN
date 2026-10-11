import { UtilDestroyService } from './../../../../../../shared/services/util-destroy.service';
import { Component, inject, OnInit } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { LayoutService } from '../../services/layout.service';
import { filter, takeUntil } from 'rxjs';
import { Utils } from '../../../../../../lib-components/src/public-api';
import { BaseConsts } from '../../../../../../lib-components/src/my-lib-components/shared/const/base.consts';
import { IUser } from '../../../../../../lib-components/src/my-lib-components/shared/interfaces/token.interface';
import { NavigationEnd, Router } from '@angular/router';

@Component({
    selector: 'app-topbar',
    standalone: false,
    templateUrl: './app-topbar.html',
    styleUrl: './app-topbar.scss',
})
export class AppTopbar implements OnInit {

    items!: MenuItem[];

    layoutService = inject(LayoutService);
    _destroyService = inject(UtilDestroyService)
    router = inject(Router)
    user: IUser

    private routeTitle = '';
    today = '';

    get pageTitle(): string {
        return this.layoutService.pageTitleOverride() || this.routeTitle;
    }

    private readonly titleMap: { match: string; title: string }[] = [];

    ngOnInit(): void {
        this.user = this.getUser();

        this.today = new Date().toLocaleDateString('vi-VN', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
        });

        this.updateTitle(this.router.url);
        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                takeUntil(this._destroyService)
            )
            .subscribe((event) => this.updateTitle((event as NavigationEnd).urlAfterRedirects));
    }

    private updateTitle(url: string) {
        this.layoutService.pageTitleOverride.set(null);
        const found = this.titleMap.find((m) => url.startsWith(m.match));
        this.routeTitle = found ? found.title : '';
    }

    getUser() {
        let userInfo = Utils.getLocalStorage(BaseConsts.localStorageUser)
        return {
            role: userInfo?.role,
            email: userInfo?.email,
            username: userInfo?.username,
            id: userInfo?.id
        }
    }

    get displayName(): string {
        return this.user?.username || this.user?.email || 'Người dùng';
    }

    get userInitial(): string {
        const source = (this.user?.username || this.user?.email || 'U').trim();
        return source.charAt(0).toUpperCase();
    }

    toggleDarkMode() {
        this.layoutService.layoutConfig.update((state) => ({
            ...state,
            darkTheme: !state.darkTheme
        }));
    }
}
