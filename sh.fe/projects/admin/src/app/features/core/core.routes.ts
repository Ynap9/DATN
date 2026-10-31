import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { AuditLogListComponent } from './audit-log/audit-log-list.component';

export const CORE_ROUTES: Routes = [
    { path: 'audit-log', component: AuditLogListComponent, canActivate: [permissionGuard], data: { permissions: [PermissionConstants.FUNCTION_AUDIT_LOG_VIEW] } },
];
