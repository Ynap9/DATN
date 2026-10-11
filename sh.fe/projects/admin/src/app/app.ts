import { Component, signal } from '@angular/core';

@Component({
    selector: 'app-root',
    standalone: false,
    template: `<router-outlet></router-outlet>`,
})
export class App {
    protected readonly title = signal('admin');
}
