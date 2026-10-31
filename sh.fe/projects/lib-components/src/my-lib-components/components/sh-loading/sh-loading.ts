import { Component, Input, Output, EventEmitter, SimpleChanges, OnChanges, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'sh-loading',
    imports: [CommonModule],
    templateUrl: './sh-loading.html',
    styleUrl: './sh-loading.scss',
})
export class ShLoadingComponent implements OnChanges, OnDestroy {
    @Input() message: string = 'Đang xử lý';
    @Input() isSpinner: boolean = false;
    @Output() isSpinnerChange = new EventEmitter<boolean>();

    visible = signal(false);
    private showTimer: any = null;

    ngOnChanges(changes: SimpleChanges) {
        if (!changes['isSpinner']) return;
        if (changes['isSpinner'].currentValue) {
            if (this.showTimer) return;
            this.showTimer = setTimeout(() => { this.visible.set(true); this.showTimer = null; }, 150);
        } else {
            if (this.showTimer) { clearTimeout(this.showTimer); this.showTimer = null; }
            this.visible.set(false);
        }
    }

    ngOnDestroy(): void {
        if (this.showTimer) { clearTimeout(this.showTimer); this.showTimer = null; }
        this.visible.set(false);
    }
}
