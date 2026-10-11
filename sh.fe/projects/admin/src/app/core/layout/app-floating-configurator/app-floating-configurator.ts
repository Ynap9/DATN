import { Component, computed, inject, input } from '@angular/core';
import { LayoutService } from '../../services/layout.service';

@Component({
    selector: 'app-floating-configurator',
    standalone: false,
    templateUrl: './app-floating-configurator.html',
    styleUrl: './app-floating-configurator.scss',
})
export class AppFloatingConfigurator {
    LayoutService = inject(LayoutService);

    float = input<boolean>(true);

    isDarkTheme = computed(() => this.LayoutService.layoutConfig().darkTheme);

    toggleDarkMode() {
        this.LayoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }
}
