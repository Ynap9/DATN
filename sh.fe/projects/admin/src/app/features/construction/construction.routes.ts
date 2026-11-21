import { CanDeactivateFn, Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { DocumentTypeDetailComponent } from './document-type/document-type-detail.component';
import { DocumentTypeListComponent } from './document-type/document-type-list.component';
import { ProjectListComponent } from './project/project-list.component';
import { ProjectReportComponent } from './project/project-report/project-report.component';
import { SubmissionListComponent } from './submission/submission-list.component';

const DOCUMENT_TYPE_PERMISSIONS = [PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW];
const PROJECT_PERMISSIONS = [PermissionConstants.MENU_PROJECT, PermissionConstants.FUNCTION_PROJECT_VIEW];
const SUBMISSION_PERMISSIONS = [PermissionConstants.MENU_SUBMISSION, PermissionConstants.FUNCTION_SUBMISSION_VIEW];

// Rời chi tiết Metadata khi tab Prompt OCR còn thay đổi chưa lưu thì hỏi trước
const metadataDetailDeactivateGuard: CanDeactivateFn<DocumentTypeDetailComponent> = (component) => component.canDeactivate();

export const CONSTRUCTION_ROUTES: Routes = [
    { path: 'project', component: ProjectListComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    { path: 'project/:id', component: ProjectReportComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    {
        path: 'project/:id/metadata/:documentTypeId',
        component: SubmissionListComponent,
        canActivate: [permissionGuard],
        data: { permissions: SUBMISSION_PERMISSIONS },
    },
    { path: 'metadata', component: DocumentTypeListComponent, canActivate: [permissionGuard], data: { permissions: DOCUMENT_TYPE_PERMISSIONS } },
    {
        path: 'metadata/:id',
        component: DocumentTypeDetailComponent,
        canActivate: [permissionGuard],
        canDeactivate: [metadataDetailDeactivateGuard],
        data: { permissions: DOCUMENT_TYPE_PERMISSIONS },
    },
];
