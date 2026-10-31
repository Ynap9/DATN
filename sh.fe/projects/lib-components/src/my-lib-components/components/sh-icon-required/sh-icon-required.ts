import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'sh-icon-required',
    imports: [CommonModule],
    templateUrl: './sh-icon-required.html',
    styleUrl: './sh-icon-required.scss',
})
export class ShIconRequiredComponent {
    @Input() isShow: boolean = true
}
