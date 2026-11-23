import { Component, computed, inject, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { IViewProjectPair } from '@/models/project.model';
import { IViewProjectMember } from '@/models/project-member.model';
import { TBL_CUSTOM_COMP_EMIT } from '@/shared/components/data-table/data-table.component';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';

// Dòng bảng thành viên: canEdit = hiện nút Sửa / Gỡ (dự án chưa đóng)
export type ProjectMemberRow = IViewProjectMember & { canEdit: boolean };

// Data mở dialog Thêm / Sửa; onMemberError báo màn danh sách tải lại cặp / bảng khi BE trả 805 / 806 / 818, dialog vẫn mở
export interface IProjectMemberDialogData {
    pair: IViewProjectPair;
    member?: ProjectMemberRow;
    onMemberError: (code: number) => void;
}

export const MEMBER_ACTIONS = {
    EDIT: 'edit',
    REMOVE: 'remove',
} as const;

// Màu vòng chữ cái và chip vai trò theo thiết kế: Leader tím, Nhập liệu xanh dương, QA xanh lá
export const MEMBER_ROLE_STYLES: Record<string, string> = {
    [MEMBER_ROLE.LEADER]: 'bg-[#efeafc] text-[#4c2d8f]',
    [MEMBER_ROLE.DATA]: 'bg-[#e3edfb] text-[#1d4580]',
    [MEMBER_ROLE.QA]: 'bg-[#e6f3ef] text-[#0b5e4e]',
};

// Chữ cái đầu của 2 từ cuối họ tên: "Trần Minh Đức" → "MĐ"; không có họ tên thì lấy tài khoản
export function getMemberInitials(member: Pick<IViewProjectMember, 'fullName' | 'userName'>) {
    const words = (member.fullName || member.userName).trim().split(/\s+/).filter(Boolean);
    return words
        .slice(-2)
        .map((word) => word[0])
        .join('')
        .toUpperCase();
}

@Component({
    selector: 'app-member-avatar',
    template: `<span class="inline-flex size-[34px] flex-none items-center justify-center rounded-full text-xs font-bold" [class]="style()" aria-hidden="true">{{ initials() }}</span>`,
})
export class MemberAvatarComponent {
    member = input.required<Pick<IViewProjectMember, 'fullName' | 'userName' | 'role'>>();

    initials = computed(() => getMemberInitials(this.member()));
    style = computed(() => MEMBER_ROLE_STYLES[this.member().role] ?? '');
}

// Các ô dưới đây là CUSTOM_COMP của DataTable: luôn nhận đủ { row, rowIndex, data }
@Component({
    selector: 'app-member-name-cell',
    imports: [MemberAvatarComponent],
    template: `
        <span class="flex items-center gap-2.5">
            <app-member-avatar [member]="row()" />
            <span class="flex min-w-0 flex-col">
                <span class="font-semibold">{{ row().fullName || row().userName }}</span>
                <span class="text-xs text-muted-color">{{ row().userName }}</span>
            </span>
        </span>
    `,
})
export class MemberNameCellComponent {
    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();
}

@Component({
    selector: 'app-member-role-cell',
    template: `<span class="rounded-full px-2.5 py-[3px] text-xs font-semibold" [class]="style()">{{ label() }}</span>`,
})
export class MemberRoleCellComponent {
    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    label = computed(() => MEMBER_ROLE_LABELS[this.row().role] ?? this.row().role);
    style = computed(() => MEMBER_ROLE_STYLES[this.row().role] ?? '');
}

// Dòng Leader không có người quản lý; null = Admin quản lý trực tiếp
@Component({
    selector: 'app-member-manager-cell',
    template: `
        @if (isLeader()) {
            <span class="text-[var(--app-faint)]">—</span>
        } @else if (row().managedByFullName) {
            {{ row().managedByFullName }}
        } @else {
            <span class="text-[var(--app-faint)]">Admin</span>
        }
    `,
})
export class MemberManagerCellComponent {
    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    isLeader = computed(() => this.row().role === MEMBER_ROLE.LEADER);
}

const PRICE_FORMAT = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 });

// Dùng cho cả Giá công ty và Giá thực nhận (data = giá trị cột); Leader không có giá
@Component({
    selector: 'app-member-price-cell',
    template: `
        @if (isLeader()) {
            <span class="text-[var(--app-faint)]">—</span>
        } @else if (priceText() === null) {
            <span class="rounded-full bg-[#fdf3e3] px-2 py-0.5 text-xs font-semibold text-[#8a5a12]">Chưa có</span>
        } @else {
            <span class="font-medium tabular-nums">{{ priceText() }} đ</span>
        }
    `,
})
export class MemberPriceCellComponent {
    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    isLeader = computed(() => this.row().role === MEMBER_ROLE.LEADER);
    priceText = computed(() => {
        const price = this.data() as number | null | undefined;
        return price === null || price === undefined ? null : PRICE_FORMAT.format(price);
    });
}

@Component({
    selector: 'app-member-status-cell',
    template: `
        <span class="inline-flex items-center gap-1.5" [class]="row().isActive ? '' : 'text-[var(--app-faint)]'">
            <span class="size-2 rounded-full" [class]="row().isActive ? 'bg-[#12a37f]' : 'bg-[#c2cac6]'"></span>
            {{ row().isActive ? 'Đang làm' : 'Tạm tắt' }}
        </span>
    `,
})
export class MemberStatusCellComponent {
    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();
}

// Nút Sửa + thùng rác phát sự kiện lên DataTable qua TBL_CUSTOM_COMP_EMIT
@Component({
    selector: 'app-member-actions-cell',
    imports: [ButtonModule],
    template: `
        @if (row().canEdit) {
            <span class="flex items-center justify-center gap-1">
                <p-button label="Sửa" size="small" severity="secondary" [outlined]="true" (onClick)="emit(actions.EDIT)" />
                <p-button icon="pi pi-trash" size="small" severity="danger" [text]="true" [ariaLabel]="'Gỡ ' + (row().fullName || row().userName)"
                    (onClick)="emit(actions.REMOVE)" />
            </span>
        }
    `,
})
export class MemberActionsCellComponent {
    private readonly _emit = inject(TBL_CUSTOM_COMP_EMIT);

    row = input.required<ProjectMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    readonly actions = MEMBER_ACTIONS;

    emit(type: string) {
        this._emit.emit({ type, row: this.row() });
    }
}
