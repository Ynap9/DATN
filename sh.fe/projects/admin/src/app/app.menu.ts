import { MenuItem } from 'primeng/api';
import { PermissionConstants } from '@/shared/constants/permission.constants';

export const APP_MENU_ITEMS: MenuItem[] = [
    {
        label: 'Tổng quan',
        items: [{ label: 'Trang chủ', icon: 'pi pi-home', routerLink: ['/admin'] }],
    },
    {
        label: 'Hồ sơ thi công',
        items: [
            { label: 'Dự án', icon: 'pi pi-building', routerLink: ['/admin/construction/project'], permission: PermissionConstants.MENU_PROJECT },
            { label: 'Cấu hình dự án', icon: 'pi pi-sliders-h', routerLink: ['/admin/construction/project-config'], permission: PermissionConstants.MENU_PROJECT_CONFIG },
            { label: 'Hồ sơ', icon: 'pi pi-folder-open', routerLink: ['/admin/construction/submission'], permission: PermissionConstants.MENU_SUBMISSION },
            { label: 'Loại văn bản', icon: 'pi pi-file', routerLink: ['/admin/construction/document-type'], permission: PermissionConstants.MENU_DOCUMENT_TYPE },
            { label: 'Trường văn bản', icon: 'pi pi-list', routerLink: ['/admin/construction/document-field'], permission: PermissionConstants.MENU_DOCUMENT_FIELD },
        ],
    },
    {
        label: 'Ký số',
        items: [
            { label: 'Mẫu chữ ký', icon: 'pi pi-pen-to-square', routerLink: ['/admin/digital-signature/template'], permission: PermissionConstants.MENU_TEMPLATE },
            { label: 'Lô ký', icon: 'pi pi-verified', routerLink: ['/admin/digital-signature/signing-batch'], permission: PermissionConstants.MENU_SIGNING_BATCH },
        ],
    },
    {
        label: 'Đóng gói',
        items: [{ label: 'Cấu hình đóng gói', icon: 'pi pi-box', routerLink: ['/admin/packaging/config'], permission: PermissionConstants.MENU_PACKAGE_CONFIG }],
    },
    {
        label: 'Vận hành',
        items: [
            { label: 'Nhóm nhân viên', icon: 'pi pi-users', routerLink: ['/admin/operation/staff-group'], permission: PermissionConstants.MENU_OPERATION_STAFF_GROUP },
            { label: 'Thành viên dự án', icon: 'pi pi-id-card', routerLink: ['/admin/operation/project-member'], permission: PermissionConstants.MENU_OPERATION_PROJECT_MEMBER },
            { label: 'Phân công', icon: 'pi pi-send', routerLink: ['/admin/operation/assignment'], permission: PermissionConstants.MENU_OPERATION_ASSIGNMENT },
        ],
    },
    {
        label: 'AI',
        items: [{ label: 'Nhà cung cấp AI', icon: 'pi pi-microchip-ai', routerLink: ['/admin/ai/provider'], permission: PermissionConstants.MENU_AI_PROVIDER }],
    },
    {
        label: 'Quản trị',
        items: [
            { label: 'Người dùng', icon: 'pi pi-user', routerLink: ['/admin/auth/user'], permission: PermissionConstants.MENU_AUTH },
            { label: 'Vai trò và phân quyền', icon: 'pi pi-shield', routerLink: ['/admin/auth/role'], permission: PermissionConstants.MENU_AUTH },
            { label: 'Nhật ký hệ thống', icon: 'pi pi-history', routerLink: ['/admin/core/audit-log'], permission: PermissionConstants.FUNCTION_AUDIT_LOG_VIEW },
        ],
    },
];
