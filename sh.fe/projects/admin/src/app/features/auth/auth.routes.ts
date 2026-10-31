import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { AccessComponent } from './access/access.component';
import { ErrorComponent } from './error/error.component';
import { LoginComponent } from './login/login.component';
import { RoleListComponent } from './role/role-list.component';
import { UserListComponent } from './user/user-list.component';

export const AUTH_PUBLIC_ROUTES: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: LoginComponent },
    { path: 'access', component: AccessComponent },
    { path: 'error', component: ErrorComponent },
];

export const AUTH_ROUTES: Routes = [
    { path: 'user', component: UserListComponent, canActivate: [permissionGuard], data: { permissions: [PermissionConstants.MENU_AUTH] } },
    { path: 'role', component: RoleListComponent, canActivate: [permissionGuard], data: { permissions: [PermissionConstants.MENU_AUTH] } },
];
