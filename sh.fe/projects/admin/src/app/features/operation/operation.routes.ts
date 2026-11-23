import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { ProjectMemberListComponent } from './project-member/project-member-list.component';

const PROJECT_MEMBER_PERMISSIONS = [PermissionConstants.MENU_OPERATION_PROJECT_MEMBER, PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE];

export const OPERATION_ROUTES: Routes = [
    { path: 'project-member', component: ProjectMemberListComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_MEMBER_PERMISSIONS } },
];
