import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { DocumentTypeDetailComponent } from './document-type/document-type-detail.component';
import { DocumentTypeListComponent } from './document-type/document-type-list.component';
import { ProjectDetailComponent } from './project/project-detail.component';
import { ProjectListComponent } from './project/project-list.component';

const DOCUMENT_TYPE_PERMISSIONS = [PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW];
const PROJECT_PERMISSIONS = [PermissionConstants.MENU_PROJECT, PermissionConstants.FUNCTION_PROJECT_VIEW];

export const CONSTRUCTION_ROUTES: Routes = [
    { path: 'project', component: ProjectListComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    { path: 'project/:id', component: ProjectDetailComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    { path: 'metadata', component: DocumentTypeListComponent, canActivate: [permissionGuard], data: { permissions: DOCUMENT_TYPE_PERMISSIONS } },
    { path: 'metadata/:id', component: DocumentTypeDetailComponent, canActivate: [permissionGuard], data: { permissions: DOCUMENT_TYPE_PERMISSIONS } },
];
