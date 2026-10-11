import { Component } from '@angular/core';
import { MenuItem } from 'primeng/api';

@Component({
    selector: 'app-menu',
    standalone: false,
    templateUrl: './app-menu.html',
    styleUrl: './app-menu.scss',
})
export class AppMenu {
    model: MenuItem[] = [];

    ngOnInit() {
        this.setMenu();
    }

    setMenu() {
        this.model = [];
    }
}
