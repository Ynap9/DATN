import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'sh-not-found',
    imports: [RouterLink, ButtonModule],
    templateUrl: './sh-not-found.html',
    styleUrl: './sh-not-found.scss',
})
export class ShNotFoundComponent {

}
