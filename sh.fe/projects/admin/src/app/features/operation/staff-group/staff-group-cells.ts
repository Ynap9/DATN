import { Component, computed, inject, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { IViewStaffGroup } from '@/models/staff-group.model';
import { TBL_CUSTOM_COMP_EMIT } from '@/shared/components/data-table/data-table.component';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';
import { MEMBER_ROLE_STYLES } from '../project-member/project-member-cells';

// Data mở dialog Tạo / Sửa; group có = Sửa
export interface IStaffGroupDialogData {
    group?: IViewStaffGroup;
}

export const STAFF_GROUP_ACTIONS = {
    EDIT: 'edit',
    REMOVE: 'remove',
} as const;

// Vai trò hệ thống một nhóm được gồm (role khác như Leader không tính)
export const STAFF_GROUP_ROLES: string[] = [MEMBER_ROLE.DATA, MEMBER_ROLE.QA];

// Chip lệch vai trò / vai trò lạ: vàng; nhóm rỗng: xám
export const MISMATCH_ROLE_STYLE = 'bg-[#fdf3e3] text-[#8a5a12]';
const EMPTY_ROLE_STYLE = 'bg-[#eef1ef] text-[#3f4b47]';

// role của BE: null = nhóm rỗng; có dấu phẩy = nhóm lệch
export function getStaffGroupRoleChip(role: string | null) {
    if (!role) return { label: 'Chưa có người', style: EMPTY_ROLE_STYLE };
    if (role.includes(',')) return { label: 'Lệch vai trò', style: MISMATCH_ROLE_STYLE };
    return { label: MEMBER_ROLE_LABELS[role] ?? role, style: MEMBER_ROLE_STYLES[role] ?? EMPTY_ROLE_STYLE };
}

// Các ô dưới đây là CUSTOM_COMP của DataTable: luôn nhận đủ { row, rowIndex, data }
@Component({
    selector: 'app-staff-group-name-cell',
    template: `
        <span class="flex min-w-0 flex-col">
            <span class="font-semibold">{{ row().name }}</span>
            <span class="text-xs text-muted-color">{{ row().description || '—' }}</span>
        </span>
    `,
})
export class StaffGroupNameCellComponent {
    row = input.required<IViewStaffGroup>();
    rowIndex = input<number>();
    data = input<unknown>();
}

@Component({
    selector: 'app-staff-group-role-cell',
    template: `<span class="rounded-full px-2.5 py-[3px] text-xs font-semibold" [class]="chip().style">{{ chip().label }}</span>`,
})
export class StaffGroupRoleCellComponent {
    row = input.required<IViewStaffGroup>();
    rowIndex = input<number>();
    data = input<unknown>();

    chip = computed(() => getStaffGroupRoleChip(this.row().role));
}

// Nút Sửa + thùng rác phát sự kiện lên DataTable qua TBL_CUSTOM_COMP_EMIT
@Component({
    selector: 'app-staff-group-actions-cell',
    imports: [ButtonModule],
    template: `
        <span class="flex items-center justify-center gap-1">
            <p-button label="Sửa" size="small" severity="secondary" [outlined]="true" (onClick)="emit(actions.EDIT)" />
            <p-button icon="pi pi-trash" size="small" severity="danger" [text]="true" [ariaLabel]="'Xoá ' + row().name"
                (onClick)="emit(actions.REMOVE)" />
        </span>
    `,
})
export class StaffGroupActionsCellComponent {
    private readonly _emit = inject(TBL_CUSTOM_COMP_EMIT);

    row = input.required<IViewStaffGroup>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly actions = STAFF_GROUP_ACTIONS;

    emit(type: string) {
        this._emit.emit({ type, row: this.row() });
    }
}
