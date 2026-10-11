import { Component, computed, effect, ElementRef, HostListener, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subject, takeUntil } from 'rxjs';
import { LayoutService } from '../../services/layout.service';
import { BaseConsts, IUser, Utils } from '../../../../../../lib-components/src/public-api';
import { AppAuthService } from '../../../../../../lib-components/src/my-lib-components/shared/services/auth/app-auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: false,
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.scss',
})
export class AppSidebar {
    layoutService = inject(LayoutService);

    router = inject(Router);

    el = inject(ElementRef);

    authService = inject(AppAuthService);

    user: IUser;

    userMenuOpen = false;

    collapsed = computed(
        () => this.layoutService.layoutState().staticMenuDesktopInactive && this.layoutService.isDesktop()
    );

    private outsideClickListener: ((event: MouseEvent) => void) | null = null;

    private destroy$ = new Subject<void>();

    constructor() {
        effect(() => {
            const state = this.layoutService.layoutState();

            if (this.layoutService.isDesktop()) {
                if (state.overlayMenuActive) {
                    this.bindOutsideClickListener();
                } else {
                    this.unbindOutsideClickListener();
                }
            } else {
                if (state.mobileMenuActive) {
                    this.bindOutsideClickListener();
                } else {
                    this.unbindOutsideClickListener();
                }
            }
        });
    }

    ngOnInit() {
        this.getUser();

        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                takeUntil(this.destroy$)
            )
            .subscribe((event) => {
                const navEvent = event as NavigationEnd;
                this.onRouteChange(navEvent.urlAfterRedirects);
            });

        this.onRouteChange(this.router.url);
    }

    ngOnDestroy() {
        this.destroy$.next();
        this.destroy$.complete();
        this.unbindOutsideClickListener();
    }

    private getUser() {
        const userInfo = Utils.getLocalStorage(BaseConsts.localStorageUser);
        this.user = {
            role: userInfo?.role,
            email: userInfo?.email,
            username: userInfo?.username,
            id: userInfo?.id,
        };
    }

    get displayName(): string {
        return this.user?.username || this.user?.email || 'Người dùng';
    }

    get userInitial(): string {
        const source = (this.user?.username || this.user?.email || 'U').trim();
        return source.charAt(0).toUpperCase();
    }

    toggleMenu() {
        this.layoutService.onMenuToggle();
    }

    toggleUserMenu(event: Event) {
        event.stopPropagation();
        this.userMenuOpen = !this.userMenuOpen;
    }

    @HostListener('document:click')
    closeUserMenu() {
        if (this.userMenuOpen) this.userMenuOpen = false;
    }

    logout() {
        this.userMenuOpen = false;
        this.authService.connectAuthorize();
    }

    private onRouteChange(path: string) {
        this.layoutService.layoutState.update((val) => ({
            ...val,
            activePath: path,
            overlayMenuActive: false,
            staticMenuMobileActive: false,
            mobileMenuActive: false,
            menuHoverActive: false
        }));
    }

    private bindOutsideClickListener() {
        if (!this.outsideClickListener) {
            this.outsideClickListener = (event: MouseEvent) => {
                if (this.isOutsideClicked(event)) {
                    this.layoutService.layoutState.update((val) => ({
                        ...val,
                        overlayMenuActive: false,
                        staticMenuMobileActive: false,
                        mobileMenuActive: false,
                        menuHoverActive: false
                    }));
                }
            };

            document.addEventListener('click', this.outsideClickListener);
        }
    }

    private unbindOutsideClickListener() {
        if (this.outsideClickListener) {
            document.removeEventListener('click', this.outsideClickListener);
            this.outsideClickListener = null;
        }
    }

    private isOutsideClicked(event: MouseEvent): boolean {
        const topbarButtonEl = document.querySelector('.layout-menu-button');
        const sidebarEl = this.el.nativeElement;

        return !(
            sidebarEl?.isSameNode(event.target as Node) ||
            sidebarEl?.contains(event.target as Node) ||
            topbarButtonEl?.isSameNode(event.target as Node) ||
            topbarButtonEl?.contains(event.target as Node)
        );
    }
}
