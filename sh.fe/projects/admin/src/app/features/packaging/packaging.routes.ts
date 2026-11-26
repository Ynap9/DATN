import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { PackageConfigComponent } from './package-config/package-config.component';

const PACKAGE_CONFIG_PERMISSIONS = [PermissionConstants.MENU_PACKAGE_CONFIG, PermissionConstants.FUNCTION_PACKAGE_CONFIG_VIEW];

export const PACKAGING_ROUTES: Routes = [
    { path: 'config', component: PackageConfigComponent, canActivate: [permissionGuard], data: { permissions: PACKAGE_CONFIG_PERMISSIONS } },
];
