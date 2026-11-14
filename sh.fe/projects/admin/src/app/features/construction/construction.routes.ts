import { Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { DocumentTypeDetailComponent } from './document-type/document-type-detail.component';
import { DocumentTypeListComponent } from './document-type/document-type-list.component';

const DOCUMENT_TYPE_PERMISSIONS = [PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW];

export const CONSTRUCTION_ROUTES: Routes = [
    { path: 'project-config', redirectTo: 'project-config/metadata', pathMatch: 'full' },
    { path: 'project-config/metadata', component: DocumentTypeListComponent, canActivate: [permissionGuard], data: { permissions: DOCUMENT_TYPE_PERMISSIONS } },
    { path: 'project-config/metadata/:id', component: DocumentTypeDetailComponent, canActivate: [permissionGuard], data: { permissions: DOCUMENT_TYPE_PERMISSIONS } },
];
