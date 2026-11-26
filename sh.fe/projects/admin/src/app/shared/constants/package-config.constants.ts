import { TagSeverity } from '@/shared/models/data-table.models';

// Theo PackageSchemaFieldConst (BE)
export const PACKAGE_FIELD_DATA_TYPES = [
    { value: 1, label: 'Văn bản' },
    { value: 2, label: 'Số' },
    { value: 3, label: 'Ngày' },
    { value: 4, label: 'Đúng/Sai' },
];

export const PACKAGE_FIELD_REQUIREMENT = {
    REQUIRED: 1,
    OPTIONAL: 2,
    CONDITIONAL: 3,
} as const;

export const PACKAGE_FIELD_REQUIREMENTS: { value: number; label: string; severity: TagSeverity }[] = [
    { value: PACKAGE_FIELD_REQUIREMENT.REQUIRED, label: 'Bắt buộc', severity: 'danger' },
    { value: PACKAGE_FIELD_REQUIREMENT.OPTIONAL, label: 'Không bắt buộc', severity: 'secondary' },
    { value: PACKAGE_FIELD_REQUIREMENT.CONDITIONAL, label: 'Có điều kiện', severity: 'warn' },
];
