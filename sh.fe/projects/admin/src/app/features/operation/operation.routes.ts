import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { AssignmentMemberListComponent } from './assignment/assignment-member-list.component';
import { ProjectMemberListComponent } from './project-member/project-member-list.component';
import { StaffGroupListComponent } from './staff-group/staff-group-list.component';
import { WageListComponent } from './wage/wage-list.component';

const ASSIGNMENT_PERMISSIONS = [PermissionConstants.MENU_OPERATION_ASSIGNMENT, PermissionConstants.FUNCTION_ASSIGNMENT_MANAGE];
const PROJECT_MEMBER_PERMISSIONS = [PermissionConstants.MENU_OPERATION_PROJECT_MEMBER, PermissionConstants.FUNCTION_PROJECT_MEMBER_MANAGE];
const STAFF_GROUP_PERMISSIONS = [PermissionConstants.MENU_OPERATION_STAFF_GROUP, PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE];

export const OPERATION_ROUTES: Routes = [
    { path: 'assignment', component: AssignmentMemberListComponent, canActivate: [permissionGuard], data: { permissions: ASSIGNMENT_PERMISSIONS } },
    { path: 'project-member', component: ProjectMemberListComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_MEMBER_PERMISSIONS } },
    { path: 'staff-group', component: StaffGroupListComponent, canActivate: [permissionGuard], data: { permissions: STAFF_GROUP_PERMISSIONS } },
    { path: 'wage', component: WageListComponent, canActivate: [permissionGuard], data: { permissions: [PermissionConstants.FUNCTION_WAGE_VIEW] } },
];
