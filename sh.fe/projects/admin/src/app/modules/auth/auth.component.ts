import {
    Component,
    OnInit,
    ViewEncapsulation,
} from '@angular/core';

@Component({
    encapsulation: ViewEncapsulation.None,
    standalone: false,
    template: `
    <div class="container-scroller">
        <div class="content-wrapper" style="min-height: 100vh;">
            <p-toast position="top-center"></p-toast>
            <router-outlet></router-outlet>
        </div>
    </div>
    `
})
export class AccountComponent implements OnInit {
    constructor() {
    }

    ngOnInit(): void {
    }
}
