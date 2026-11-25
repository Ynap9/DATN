import { Component, inject, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { IAssignmentItem, IAssignmentTicket } from '@/models/assignment.model';
import { IViewProjectPair } from '@/models/project.model';
import { IViewProjectMember } from '@/models/project-member.model';
import { TBL_CUSTOM_COMP_EMIT } from '@/shared/components/data-table/data-table.component';
import { AssignmentRole } from '@/shared/constants/assignment.constants';

// Dòng bảng người: assignDisabledReason rỗng = bấm được Chia phiếu, có chữ = khoá + tooltip
export type AssignmentMemberRow = IViewProjectMember & { assignDisabledReason: string };

export interface IAssignDialogData {
    pair: IViewProjectPair;
    member: AssignmentMemberRow;
    role: AssignmentRole;
}

// Kết quả đóng dialog: đã giao / cần tải lại bảng (811, 821) / cần tải lại cặp (818, 805); undefined = đóng tay
export const ASSIGN_DIALOG_RESULT = {
    ASSIGNED: 'assigned',
    RELOAD_MEMBERS: 'reload-members',
    RELOAD_PAIRS: 'reload-pairs',
} as const;

export const ASSIGNMENT_ACTIONS = {
    TICKETS: 'tickets',
    ASSIGN: 'assign',
} as const;

// Dialog phiếu của một người: isClosed = dự án DONE / EXPIRED (chỉ xem)
export interface ITicketDialogData {
    pair: IViewProjectPair;
    member: AssignmentMemberRow;
    role: AssignmentRole;
    isClosed: boolean;
}

// Đóng dialog phiếu: đã chuyển / thu hồi (tải lại bảng người) / cần tải lại cặp (818, 805); undefined = không đổi gì
export const TICKET_DIALOG_RESULT = {
    CHANGED: 'changed',
    RELOAD_PAIRS: 'reload-pairs',
} as const;

export const MOVE_MODE = {
    TRANSFER: 'transfer',
    REVOKE: 'revoke',
} as const;

export type MoveMode = (typeof MOVE_MODE)[keyof typeof MOVE_MODE];

// items = nút của phiếu (API 24) nếu dialog phiếu đã tải, không thì dialog chuyển / thu hồi tự tải
export interface IMoveDialogData {
    pair: IViewProjectPair;
    member: AssignmentMemberRow;
    role: AssignmentRole;
    ticket: IAssignmentTicket;
    mode: MoveMode;
    items: IAssignmentItem[] | null;
}

// Đóng dialog chuyển / thu hồi: xong / phiếu đã đổi (815, 816, 817) → tải lại bảng phiếu / dự án đóng (818, 805) → tải lại cặp
export const MOVE_DIALOG_RESULT = {
    DONE: 'done',
    RELOAD_TICKETS: 'reload-tickets',
    RELOAD_PAIRS: 'reload-pairs',
} as const;

// Các ô dưới đây là CUSTOM_COMP của DataTable: luôn nhận đủ { row, rowIndex, data }
@Component({
    selector: 'app-assignment-name-cell',
    template: `<span class="font-semibold">{{ row().fullName || '—' }}</span>`,
})
export class AssignmentNameCellComponent {
    row = input.required<AssignmentMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();
}

@Component({
    selector: 'app-assignment-status-cell',
    template: `
        <span class="rounded-full px-2.5 py-[3px] text-xs font-semibold"
            [class]="row().isActive ? 'bg-[#e6f3ef] text-[#0b5e4e]' : 'bg-[#eef1ef] text-[#5d6b66]'">
            {{ row().isActive ? 'Đang hoạt động' : 'Đã tắt' }}
        </span>
    `,
})
export class AssignmentStatusCellComponent {
    row = input.required<AssignmentMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();
}

// Phiếu luôn bấm được (người tắt / dự án đóng vẫn xem); nút Chia phiếu khoá vẫn cần tooltip: bọc span vì nút disabled không nhận hover
@Component({
    selector: 'app-assignment-actions-cell',
    imports: [ButtonModule, TooltipModule],
    template: `
        <span class="flex justify-center gap-2">
            <p-button label="Phiếu" size="small" severity="secondary" [outlined]="true" (onClick)="emit(actions.TICKETS)" />
            <span [pTooltip]="row().assignDisabledReason" tooltipPosition="left">
                <p-button label="Chia phiếu" size="small" [disabled]="!!row().assignDisabledReason" (onClick)="emit(actions.ASSIGN)" />
            </span>
        </span>
    `,
})
export class AssignmentActionsCellComponent {
    private readonly _emit = inject(TBL_CUSTOM_COMP_EMIT);

    readonly actions = ASSIGNMENT_ACTIONS;
    row = input.required<AssignmentMemberRow>();
    rowIndex = input<number>();
    data = input<unknown>();

    emit(type: string) {
        this._emit.emit({ type, row: this.row() });
    }
}
