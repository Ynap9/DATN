import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize, Observable } from 'rxjs';
import { IAssignmentChangeResult, IAssignmentItem } from '@/models/assignment.model';
import { AssignmentService } from '@/services/assignment.service';
import { ProjectMemberService } from '@/services/project-member.service';
import { BaseComponent } from '@/shared/base/base.component';
import { ASSIGNMENT_ERROR_CODE, ASSIGNMENT_ROLE } from '@/shared/constants/assignment.constants';
import { IBaseResponse, IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { SharedImports } from '@/shared/import.shared';
import { getBoxLabel, getProfileLabel } from '../assign-dialog/assign-dialog.component';
import { IMoveDialogData, MOVE_DIALOG_RESULT, MOVE_MODE } from '../assignment-cells';

const INVALID_REQUEST = 'Dữ liệu gửi lên không hợp lệ';
// Người nhận tải một lần, tối đa BE cho phép; lọc ngay trong ô chọn
const RECIPIENT_PAGE_SIZE = 200;

// Phiếu đã đổi so với lúc mở → đóng và tải lại bảng phiếu; còn lại (812, 811, 821, 331, 825, 329) giữ dialog để thử lại / chọn người khác
const RELOAD_TICKET_CODES: number[] = [
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_NOTHING_TO_REVOKE,
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_TICKET_NOT_ACTIVE,
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_TICKET_NOT_FOUND,
];

const SCOPE = { ALL: 'all', ITEMS: 'items' } as const;

const NOTES = {
    [ASSIGNMENT_ROLE.DATA]: {
        [MOVE_MODE.TRANSFER]: (name: string) => `Chỉ chuyển văn bản chưa kiểm. Văn bản đã kiểm và văn bản đang bị QA trả sửa ở lại ${name}.`,
        [MOVE_MODE.REVOKE]: (name: string) =>
            `Chỉ lấy lại văn bản chưa kiểm, chúng về "Chưa giao" trên cây. Văn bản đã kiểm và đang bị QA trả sửa ở lại ${name}.`,
    },
    [ASSIGNMENT_ROLE.QA]: {
        [MOVE_MODE.TRANSFER]: (name: string) =>
            `Chỉ chuyển văn bản chưa QA đạt trong các nút. Văn bản đã đạt giữ nguyên ${name}. Văn bản kiểm xong sau này tự về người nhận.`,
        [MOVE_MODE.REVOKE]: (name: string) => `Chỉ lấy lại văn bản chưa QA đạt, chúng về "Chưa giao" trên cây QA. Văn bản đã đạt giữ nguyên ${name}.`,
    },
};

interface IRecipientOption {
    userId: string;
    name: string;
    label: string;
}

@Component({
    selector: 'app-move-dialog',
    imports: [SharedImports, ShSelectComponent],
    templateUrl: './move-dialog.component.html',
})
export class MoveDialogComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IMoveDialogData = inject(DynamicDialogConfig).data;
    private readonly _assignmentService = inject(AssignmentService);
    private readonly _projectMemberService = inject(ProjectMemberService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly pair = this._data.pair;
    readonly member = this._data.member;
    readonly ticket = this._data.ticket;
    readonly isQa = this._data.role === ASSIGNMENT_ROLE.QA;
    readonly isTransfer = this._data.mode === MOVE_MODE.TRANSFER;
    readonly scopes = SCOPE;
    readonly memberName = this.member.fullName || this.member.userName;
    readonly title = `${this.isTransfer ? 'Chuyển' : 'Thu hồi'} ${this.isQa ? 'phụ trách' : 'phiếu'} #${this.ticket.id}`;
    readonly subtitle = `Của ${this.memberName} · ${this.ticket.documentCount} văn bản`;
    readonly note = NOTES[this._data.role][this._data.mode](this.memberName);
    readonly recipientId = new FormControl<string | null>(null);

    scope: string = SCOPE.ALL;
    items: IAssignmentItem[] | null = this._data.items;
    loadingItems = false;
    selectedIndexes = new Set<number>();
    recipients: IRecipientOption[] = [];
    loadingRecipients = false;
    submitting = false;

    get canSubmit() {
        if (this.submitting) return false;
        if (this.scope === SCOPE.ITEMS && !this.selectedIndexes.size) return false;
        return !this.isTransfer || !!this.recipientId.value;
    }

    override ngOnInit(): void {
        if (this.isTransfer) {
            this.loadRecipients();
        }
    }

    getItemLabel(item: IAssignmentItem) {
        return `${getBoxLabel(item.box)} › ${getProfileLabel(item.profileRange)}`;
    }

    onScopeChange(scope: string) {
        this.scope = scope;
        if (scope === SCOPE.ITEMS && !this.items && !this.loadingItems) {
            this.loadItems();
        }
    }

    toggleItem(index: number) {
        if (!this.selectedIndexes.delete(index)) {
            this.selectedIndexes.add(index);
        }
    }

    submit() {
        if (!this.canSubmit) return;
        this.submitting = true;
        this.buildRequest()
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res)) return this.handleError(res);
                    this.messageSuccess(this.getSuccessMessage(res.data));
                    this._ref.close(MOVE_DIALOG_RESULT.DONE);
                },
                error: (error: HttpErrorResponse) => this.messageError(error.status === 400 ? INVALID_REQUEST : ''),
            });
    }

    close() {
        this._ref.close();
    }

    // items lấy nguyên { box, profileRange } của API 24; cả phiếu thì Nhập liệu gửi ticketId, QA không gửi items
    private buildRequest(): Observable<IBaseResponseWithData<IAssignmentChangeResult>> {
        const { projectId, documentTypeId } = this.pair;
        const items = this.scope === SCOPE.ITEMS ? (this.items ?? []).filter((_, index) => this.selectedIndexes.has(index)) : undefined;
        const toUserId = this.recipientId.value ?? '';
        if (this.isQa) {
            return this.isTransfer
                ? this._assignmentService.transferQa(projectId, documentTypeId, { ticketId: this.ticket.id, toUserId, items })
                : this._assignmentService.revokeQa(projectId, documentTypeId, { ticketId: this.ticket.id, items });
        }
        const target = items ? { items } : { ticketId: this.ticket.id };
        return this.isTransfer
            ? this._assignmentService.transfer(projectId, documentTypeId, { fromUserId: this.member.userId, toUserId, ...target })
            : this._assignmentService.revoke(projectId, documentTypeId, items ? { assigneeUserId: this.member.userId, items } : target);
    }

    private getSuccessMessage(result: IAssignmentChangeResult) {
        if (!this.isTransfer) return `Đã thu hồi ${result.affectedDocuments} văn bản của ${this.memberName}`;
        const recipient = this.recipients.find((option) => option.userId === this.recipientId.value);
        const ticketText = result.newTicket ? `, phiếu #${result.newTicket.id}` : '';
        return `Đã chuyển ${result.affectedDocuments} văn bản sang ${recipient?.name ?? ''}${ticketText}`;
    }

    // Toast câu BE đã hiện ở isResponseSucceed; ở đây chỉ quyết định đóng hay giữ dialog
    private handleError(res: IBaseResponse) {
        const code = res.code;
        if (code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_PROJECT_CLOSED || code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_NOT_PROJECT_LEADER) {
            this._ref.close(MOVE_DIALOG_RESULT.RELOAD_PAIRS);
        } else if (RELOAD_TICKET_CODES.includes(code)) {
            this._ref.close(MOVE_DIALOG_RESULT.RELOAD_TICKETS);
        }
    }

    private loadItems() {
        this.loadingItems = true;
        this._assignmentService
            .findTicket(this.pair.projectId, this.pair.documentTypeId, this.ticket.id)
            .pipe(
                finalize(() => (this.loadingItems = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this.handleError(res);
                this.items = res.data.items;
            });
    }

    // Người nhận: thành viên cùng vai trò đang hoạt động, trừ chính người đó (Leader: BE chỉ trả người mình quản lý)
    private loadRecipients() {
        this.loadingRecipients = true;
        this._projectMemberService
            .findPaging(this.pair.projectId, {
                documentTypeId: this.pair.documentTypeId,
                role: this._data.role,
                isActive: true,
                pageNumber: this.START_PAGE_NUMBER,
                pageSize: RECIPIENT_PAGE_SIZE,
            })
            .pipe(
                finalize(() => (this.loadingRecipients = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                this.recipients = res.data.items
                    .filter((member) => member.userId !== this.member.userId)
                    .map((member) => {
                        const name = member.fullName || member.userName;
                        return { userId: member.userId, name, label: `${name} (${member.userName})` };
                    });
            });
    }
}
