import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DynamicDialogConfig } from 'primeng/dynamicdialog';
import { finalize } from 'rxjs';
import { IAuditLogDetail } from '@/models/audit-log.model';
import { AuditLogService } from '@/services/audit-log.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

@Component({
    selector: 'app-audit-log-detail',
    imports: [SharedImports],
    templateUrl: './audit-log-detail.component.html',
    styleUrl: './audit-log-detail.component.scss',
})
export class AuditLogDetailComponent extends BaseComponent {
    private readonly _config = inject(DynamicDialogConfig);
    private readonly _auditLogService = inject(AuditLogService);
    private readonly _destroyRef = inject(DestroyRef);

    log: IAuditLogDetail | null = null;

    override ngOnInit(): void {
        this.loading = true;
        this._auditLogService
            .findById(this._config.data.id)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.log = res.data;
                }
            });
    }

    formatJson(value?: string): string {
        if (!value) return '—';

        try {
            return JSON.stringify(JSON.parse(value), null, 2);
        } catch {
            return value;
        }
    }
}
