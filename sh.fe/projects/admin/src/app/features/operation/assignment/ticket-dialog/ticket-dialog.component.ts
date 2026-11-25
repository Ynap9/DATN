import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { finalize } from 'rxjs';
import { IAssignmentItem, IAssignmentTicket } from '@/models/assignment.model';
import { AssignmentService } from '@/services/assignment.service';
import { BaseComponent } from '@/shared/base/base.component';
import {
    ASSIGNMENT_ERROR_CODE,
    ASSIGNMENT_ROLE,
    ASSIGNMENT_STATUS,
    ASSIGNMENT_STATUS_LABELS,
    ASSIGNMENT_TICKET_PAGE_SIZE,
} from '@/shared/constants/assignment.constants';
import { SharedImports } from '@/shared/import.shared';
import { getBoxLabel, getProfileLabel } from '../assign-dialog/assign-dialog.component';
import { IMoveDialogData, ITicketDialogData, MOVE_DIALOG_RESULT, MOVE_MODE, MoveMode, TICKET_DIALOG_RESULT } from '../assignment-cells';
import { MoveDialogComponent } from '../move-dialog/move-dialog.component';

// Lọc trạng thái phiếu: mặc định Đang làm; "Mọi trạng thái" = không gửi status
const ALL_STATUS = 'all';
const STATUS_OPTIONS = [
    { value: String(ASSIGNMENT_STATUS.ACTIVE), label: ASSIGNMENT_STATUS_LABELS[ASSIGNMENT_STATUS.ACTIVE] },
    { value: String(ASSIGNMENT_STATUS.DONE), label: ASSIGNMENT_STATUS_LABELS[ASSIGNMENT_STATUS.DONE] },
    { value: String(ASSIGNMENT_STATUS.CANCELLED), label: ASSIGNMENT_STATUS_LABELS[ASSIGNMENT_STATUS.CANCELLED] },
    { value: ALL_STATUS, label: 'Mọi trạng thái' },
];

@Component({
    selector: 'app-ticket-dialog',
    imports: [SharedImports, PaginatorModule, ShSelectComponent],
    templateUrl: './ticket-dialog.component.html',
})
export class TicketDialogComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: ITicketDialogData = inject(DynamicDialogConfig).data;
    private readonly _assignmentService = inject(AssignmentService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly pair = this._data.pair;
    readonly member = this._data.member;
    readonly role = this._data.role;
    readonly isClosed = this._data.isClosed;
    readonly isQa = this.role === ASSIGNMENT_ROLE.QA;
    readonly statusOptions = STATUS_OPTIONS;
    // "Mọi trạng thái": thêm nhãn trạng thái cạnh số phiếu để phân biệt
    readonly allStatus = ALL_STATUS;
    readonly pageSize = ASSIGNMENT_TICKET_PAGE_SIZE;
    readonly moveMode = MOVE_MODE;
    readonly memberName = this.member.fullName || this.member.userName;
    readonly subtitle = this.isQa
        ? 'QA phụ trách · Chuyển / thu hồi chỉ lấy văn bản chưa QA đạt'
        : 'Nhập liệu · Chuyển / thu hồi chỉ lấy văn bản chưa kiểm';

    statusFilter = String(ASSIGNMENT_STATUS.ACTIVE);
    pageNumber = this.START_PAGE_NUMBER;
    tickets: IAssignmentTicket[] = [];
    override totalRecords = 0;
    // Nút của phiếu (API 24) tải một lần / phiếu khi mở Chi tiết
    expandedIds = new Set<number>();
    itemsByTicket = new Map<number, IAssignmentItem[]>();
    loadingItems = new Set<number>();
    // Đã chuyển / thu hồi ít nhất một lần → bảng người cần tải lại khi đóng
    private _hasChanged = false;

    override ngOnInit(): void {
        this.loadTickets();
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với giá trị đang lọc
    onStatusChange(value: string | null) {
        const statusFilter = value || String(ASSIGNMENT_STATUS.ACTIVE);
        if (statusFilter === this.statusFilter) return;
        this.statusFilter = statusFilter;
        this.pageNumber = this.START_PAGE_NUMBER;
        this.loadTickets();
    }

    onPageChange(event: PaginatorState) {
        this.pageNumber = (event.page ?? 0) + 1;
        this.loadTickets();
    }

    // Chuyển / Thu hồi chỉ phiếu đang làm của dự án chưa đóng (BE 816 / 818)
    canMove(ticket: IAssignmentTicket) {
        return ticket.status === ASSIGNMENT_STATUS.ACTIVE && !this.isClosed;
    }

    getStatusLabel(ticket: IAssignmentTicket) {
        return ASSIGNMENT_STATUS_LABELS[ticket.status] ?? String(ticket.status);
    }

    getCheckedPercent(ticket: IAssignmentTicket) {
        return ticket.total ? Math.round((ticket.checked / ticket.total) * 100) : 0;
    }

    getItemLabel(item: IAssignmentItem) {
        return `${getBoxLabel(item.box)} › ${getProfileLabel(item.profileRange)}`;
    }

    getItems(ticket: IAssignmentTicket) {
        return this.itemsByTicket.get(ticket.id) ?? [];
    }

    toggleDetail(ticket: IAssignmentTicket) {
        if (this.expandedIds.delete(ticket.id)) return;
        this.expandedIds.add(ticket.id);
        if (this.itemsByTicket.has(ticket.id) || this.loadingItems.has(ticket.id)) return;
        this.loadingItems.add(ticket.id);
        this._assignmentService
            .findTicket(this.pair.projectId, this.pair.documentTypeId, ticket.id)
            .pipe(
                finalize(() => this.loadingItems.delete(ticket.id)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    this.expandedIds.delete(ticket.id);
                    return;
                }
                this.itemsByTicket.set(ticket.id, res.data.items);
            });
    }

    openMove(ticket: IAssignmentTicket, mode: MoveMode) {
        const data: IMoveDialogData = {
            pair: this.pair,
            member: this.member,
            role: this.role,
            ticket,
            mode,
            items: this.itemsByTicket.get(ticket.id) ?? null,
        };
        this._dialogService
            .open(MoveDialogComponent, { width: '520px', modal: true, showHeader: false, closable: false, closeOnEscape: false, data })
            ?.onClose.subscribe((result) => {
                if (result === MOVE_DIALOG_RESULT.RELOAD_PAIRS) {
                    this._ref.close(TICKET_DIALOG_RESULT.RELOAD_PAIRS);
                } else if (result) {
                    this._hasChanged = this._hasChanged || result === MOVE_DIALOG_RESULT.DONE;
                    this.loadTickets();
                }
            });
    }

    close() {
        this._ref.close(this._hasChanged ? TICKET_DIALOG_RESULT.CHANGED : undefined);
    }

    // Tải lại bảng thì bỏ nút đã nhớ: phiếu có thể vừa mất nút sau chuyển / thu hồi theo hộp / hồ sơ
    private loadTickets() {
        this.loading = true;
        this.expandedIds.clear();
        this.itemsByTicket.clear();
        this._assignmentService
            .findTicketPaging(this.pair.projectId, this.pair.documentTypeId, {
                role: this.role,
                assigneeUserId: this.member.userId,
                status: this.statusFilter === ALL_STATUS ? undefined : Number(this.statusFilter),
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
            })
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    if (res.code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_NOT_PROJECT_LEADER) {
                        this._ref.close(TICKET_DIALOG_RESULT.RELOAD_PAIRS);
                    }
                    return;
                }
                this.tickets = res.data.items;
                this.totalRecords = res.data.totalItems;
            });
    }
}
