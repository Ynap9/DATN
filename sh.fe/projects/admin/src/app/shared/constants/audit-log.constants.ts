import { TagSeverity } from '@/shared/models/data-table.models';

export const AUDIT_LOG_ACTION_TYPES: { value: string; label: string; severity: TagSeverity }[] = [
    { value: 'Create', label: 'Tạo mới', severity: 'success' },
    { value: 'Update', label: 'Cập nhật', severity: 'info' },
    { value: 'Delete', label: 'Xóa', severity: 'danger' },
    { value: 'Login', label: 'Đăng nhập', severity: 'secondary' },
    { value: 'Export', label: 'Xuất dữ liệu', severity: 'warn' },
];
