import { CanDeactivateFn, Routes } from '@angular/router';
import { permissionGuard } from '@/core/auth/guard/permission.guard';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { DataTransferComponent } from './data-transfer/data-transfer.component';
import { DocumentTypeDetailComponent } from './document-type/document-type-detail.component';
import { DocumentTypeListComponent } from './document-type/document-type-list.component';
import { ProjectListComponent } from './project/project-list.component';
import { ProjectReportComponent } from './project/project-report/project-report.component';
import { SubmissionCheckComponent } from './submission/submission-check.component';
import { SubmissionListComponent } from './submission/submission-list.component';
import { SubmissionQaComponent } from './submission/submission-qa/submission-qa.component';

const DOCUMENT_TYPE_PERMISSIONS = [PermissionConstants.MENU_DOCUMENT_TYPE, PermissionConstants.FUNCTION_DOCUMENT_TYPE_VIEW];
const PROJECT_PERMISSIONS = [PermissionConstants.MENU_PROJECT, PermissionConstants.FUNCTION_PROJECT_VIEW];
const SUBMISSION_PERMISSIONS = [PermissionConstants.MENU_SUBMISSION, PermissionConstants.FUNCTION_SUBMISSION_VIEW];
const DATA_TRANSFER_PERMISSIONS = [PermissionConstants.FUNCTION_SUBMISSION_IMPORT, PermissionConstants.FUNCTION_SUBMISSION_EXPORT];

// Rời chi tiết Metadata khi tab Prompt OCR còn thay đổi chưa lưu thì hỏi trước
const metadataDetailDeactivateGuard: CanDeactivateFn<DocumentTypeDetailComponent> = (component) => component.canDeactivate();
// Rời màn Nhập / Xuất dữ liệu khi đang import Google thì hỏi trước
const dataTransferDeactivateGuard: CanDeactivateFn<DataTransferComponent> = (component) => component.canDeactivate();
// Rời màn Nhập liệu khi còn sửa chưa lưu thì hỏi trước
const submissionCheckDeactivateGuard: CanDeactivateFn<SubmissionCheckComponent> = (component) => component.canDeactivate();
// Rời màn QA khi còn kết quả đang chấm dở thì hỏi trước
const submissionQaDeactivateGuard: CanDeactivateFn<SubmissionQaComponent> = (component) => component.canDeactivate();

export const CONSTRUCTION_ROUTES: Routes = [
    { path: 'project', component: ProjectListComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    { path: 'project/:id', component: ProjectReportComponent, canActivate: [permissionGuard], data: { permissions: PROJECT_PERMISSIONS } },
    {
        path: 'project/:id/metadata/:documentTypeId',
        component: SubmissionListComponent,
        canActivate: [permissionGuard],
        data: { permissions: SUBMISSION_PERMISSIONS },
    },
    {
        path: 'project/:id/metadata/:documentTypeId/check/:submissionId',
        component: SubmissionCheckComponent,
        canActivate: [permissionGuard],
        canDeactivate: [submissionCheckDeactivateGuard],
        data: { permissions: SUBMISSION_PERMISSIONS },
    },
    {
        path: 'project/:id/metadata/:documentTypeId/qa/:submissionId',
        component: SubmissionQaComponent,
        canActivate: [permissionGuard],
        canDeactivate: [submissionQaDeactivateGuard],
        data: { permissions: SUBMISSION_PERMISSIONS },
    },
    {
        path: 'data-transfer',
        component: DataTransferComponent,
        canActivate: [permissionGuard],
        canDeactivate: [dataTransferDeactivateGuard],
        data: { permissions: DATA_TRANSFER_PERMISSIONS },
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
