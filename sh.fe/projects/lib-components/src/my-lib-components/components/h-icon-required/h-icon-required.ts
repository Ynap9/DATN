import { Component, Input } from '@angular/core';

@Component({
    selector: 'h-icon-required',
    standalone: false,
    templateUrl: './h-icon-required.html',
    styleUrl: './h-icon-required.scss',
})
export class HIconRequiredComponent {
    @Input() isShow: boolean = true
}
