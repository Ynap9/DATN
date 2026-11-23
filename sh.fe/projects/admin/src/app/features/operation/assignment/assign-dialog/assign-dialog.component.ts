import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { debounceTime, finalize } from 'rxjs';
import { IAssignmentConflict, IAssignmentItem, IAssignmentNode, IAssignmentPreview, IAssignmentRequest } from '@/models/assignment.model';
import { AssignmentService } from '@/services/assignment.service';
import { BaseComponent } from '@/shared/base/base.component';
import { ASSIGNMENT_ERROR_CODE, ASSIGNMENT_ITEMS_MAX, ASSIGNMENT_ROLE, ASSIGNMENT_TREE_PAGE_SIZE } from '@/shared/constants/assignment.constants';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { SharedImports } from '@/shared/import.shared';
import { ASSIGN_DIALOG_RESULT, IAssignDialogData } from '../assignment-cells';

const SEARCH_DEBOUNCE_MS = 400;
// Hồ sơ của một hộp tải một lần, tối đa BE cho phép
const PROFILE_PAGE_SIZE = 200;

// Khoá tick: hộp / hồ sơ so khớp không phân biệt hoa thường như BE; "*" = cả hộp, "=" + tên = một hồ sơ
const KEY_SEPARATOR = '\u0001';
const BOX_ITEM_MARK = '*';
const PROFILE_ITEM_MARK = '=';

// Lỗi khi Giao do dữ liệu đổi sau lần xem trước → tự xem trước lại để hiện lý do mới
const RETRY_PREVIEW_CODES: number[] = [
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_ASSIGNMENT_CONFLICT,
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_ASSIGNMENT_NODE_EMPTY,
    ASSIGNMENT_ERROR_CODE.CONSTRUCTION_ERROR_QA_SAME_AS_ENTRY,
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_NOTHING_TO_ASSIGN,
    ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_TICKET_TOO_LARGE,
    ASSIGNMENT_ERROR_CODE.CONSTRUCTION_ERROR_QA_TICKET_TOO_LARGE,
];

type CheckState = 'checked' | 'partial' | 'none';

// unassigned lấy lúc tick, để đếm "Chưa giao trong nút đã chọn" trên số đã tải
interface ISelectedNode extends IAssignmentItem {
    unassigned: number;
}

const toBoxKey = (box: string) => box.toLowerCase();
const toBoxItemKey = (box: string) => toBoxKey(box) + KEY_SEPARATOR + BOX_ITEM_MARK;
const toProfileKey = (box: string, profileRange: string) => toBoxKey(box) + KEY_SEPARATOR + PROFILE_ITEM_MARK + profileRange.toLowerCase();

export const getBoxLabel = (box: string) => (box ? `Hộp ${box}` : '(Không có hộp)');
export const getProfileLabel = (profileRange: string | null) => profileRange || '(Không có hồ sơ)';

@Component({
    selector: 'app-assign-dialog',
    imports: [SharedImports, PaginatorModule, IconFieldModule, InputIconModule],
    templateUrl: './assign-dialog.component.html',
})
export class AssignDialogComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IAssignDialogData = inject(DynamicDialogConfig).data;
    private readonly _assignmentService = inject(AssignmentService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly pair = this._data.pair;
    readonly member = this._data.member;
    readonly role = this._data.role;
    readonly isQa = this.role === ASSIGNMENT_ROLE.QA;
    readonly treePageSize = ASSIGNMENT_TREE_PAGE_SIZE;
    readonly getBoxLabel = getBoxLabel;
    readonly getProfileLabel = getProfileLabel;
    readonly keyword = new FormControl('', { nonNullable: true });
    readonly memberName = this.member.fullName || this.member.userName;
    readonly title = this.isQa ? `Giao QA phụ trách — ${this.memberName}` : `Chia phiếu cho ${this.memberName}`;
    readonly subtitle = this.isQa
        ? `${this.member.userName} · cây chỉ gồm hộp / hồ sơ có văn bản đã kiểm`
        : `${this.member.userName} · Nhập liệu · đang làm ${this.member.heldTicketCount} phiếu`;

    onlyUnassigned = true;
    boxes: IAssignmentNode[] = [];
    totalBoxes = 0;
    boxPage = this.START_PAGE_NUMBER;
    loadingTree = false;
    // Hồ sơ đã tải theo hộp: giữ khi đổi trang / đổi lọc
    expandedBoxes = new Set<string>();
    profilesByBox = new Map<string, IAssignmentNode[]>();
    loadingProfiles = new Set<string>();

    // Tick giữ qua đổi trang / đổi lọc
    selection = new Map<string, ISelectedNode>();
    selectedUnassigned = 0;
    private _partialBoxes = new Set<string>();

    // Kết quả xem trước; đổi tick là xoá, phải xem lại mới Giao được
    preview: IAssignmentPreview | null = null;
    previewing = false;
    assigning = false;

    get boxRangeText() {
        if (!this.totalBoxes) return 'Không có hộp';
        const from = (this.boxPage - 1) * this.treePageSize + 1;
        const to = Math.min(this.boxPage * this.treePageSize, this.totalBoxes);
        return `Hộp ${from}–${to} / ${this.totalBoxes} · sắp tự nhiên theo tên`;
    }

    get canAssign() {
        return !!this.preview?.canSave && !this.previewing && !this.assigning;
    }

    // Số trên khung xem trước theo vai trò; danger = tô đỏ khi > 0
    get previewMetrics() {
        const preview = this.preview;
        if (!preview) return [];
        if (!this.isQa) {
            return [
                { label: 'Sẽ giao', value: preview.newDocuments, danger: false },
                { label: 'Đã của người này', value: preview.alreadyHeldDocuments, danger: false },
                { label: 'Người khác đang giữ', value: preview.conflictDocuments, danger: true },
            ];
        }
        return [
            { label: 'Đã kiểm, giao QA ngay', value: preview.newDocuments, danger: false },
            { label: 'Còn đang nhập, kiểm xong tự về QA này', value: preview.laterDocuments, danger: false },
            { label: 'Đã của người này', value: preview.alreadyHeldDocuments, danger: false },
            { label: 'Người khác đang giữ', value: preview.conflictDocuments, danger: true },
            { label: 'QA từng nhập / đang nhập văn bản', value: preview.sameAsEntryDocuments, danger: true },
        ];
    }

    override ngOnInit(): void {
        this.keyword.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this._destroyRef)).subscribe(() => {
            this.boxPage = this.START_PAGE_NUMBER;
            this.loadTree();
        });
        this.loadTree();
    }

    onOnlyUnassignedChange(value: boolean) {
        this.onlyUnassigned = value;
        this.boxPage = this.START_PAGE_NUMBER;
        this.loadTree();
    }

    onBoxPageChange(event: PaginatorState) {
        this.boxPage = (event.page ?? 0) + 1;
        this.loadTree();
    }

    isExpanded(box: string) {
        return this.expandedBoxes.has(toBoxKey(box));
    }

    getProfiles(box: string) {
        return this.profilesByBox.get(toBoxKey(box)) ?? [];
    }

    isLoadingProfiles(box: string) {
        return this.loadingProfiles.has(toBoxKey(box));
    }

    toggleExpand(node: IAssignmentNode) {
        const boxKey = toBoxKey(node.box);
        if (this.expandedBoxes.delete(boxKey)) return;
        this.expandedBoxes.add(boxKey);
        if (!this.profilesByBox.has(boxKey)) {
            this.loadProfiles(node.box);
        }
    }

    getBoxState(box: string): CheckState {
        if (this.selection.has(toBoxItemKey(box))) return 'checked';
        return this._partialBoxes.has(toBoxKey(box)) ? 'partial' : 'none';
    }

    isProfileChecked(box: string, profileRange: string | null) {
        return this.selection.has(toBoxItemKey(box)) || this.selection.has(toProfileKey(box, profileRange ?? ''));
    }

    // Tick hộp = cả hộp (bỏ mọi tick hồ sơ con); hộp đang tick → bỏ
    toggleBox(node: IAssignmentNode, checkbox: HTMLInputElement) {
        const boxItemKey = toBoxItemKey(node.box);
        if (this.selection.has(boxItemKey)) {
            this.selection.delete(boxItemKey);
        } else {
            const childKeys = this.getChildProfileKeys(node.box);
            if (this.selection.size - childKeys.length + 1 > ASSIGNMENT_ITEMS_MAX) {
                this.warnMaxItems();
            } else {
                childKeys.forEach((key) => this.selection.delete(key));
                this.selection.set(boxItemKey, { box: node.box, profileRange: null, unassigned: node.unassigned });
            }
        }
        this.afterSelectionChange();
        this.syncCheckbox(checkbox, this.getBoxState(node.box));
    }

    // Hộp đang tick mà bỏ một hồ sơ → thành tick các hồ sơ còn lại (hồ sơ đang hiện nên đã tải)
    toggleProfile(boxNode: IAssignmentNode, profile: IAssignmentNode, checkbox: HTMLInputElement) {
        const boxItemKey = toBoxItemKey(boxNode.box);
        const profileKey = toProfileKey(boxNode.box, profile.profileRange ?? '');
        if (this.selection.has(boxItemKey)) {
            const siblings = this.getProfiles(boxNode.box).filter((item) => item !== profile);
            if (this.selection.size - 1 + siblings.length > ASSIGNMENT_ITEMS_MAX) {
                this.warnMaxItems();
            } else {
                this.selection.delete(boxItemKey);
                siblings.forEach((item) => this.selectProfile(boxNode.box, item));
            }
        } else if (this.selection.has(profileKey)) {
            this.selection.delete(profileKey);
        } else if (this.selection.size + 1 > ASSIGNMENT_ITEMS_MAX) {
            this.warnMaxItems();
        } else {
            this.selectProfile(boxNode.box, profile);
        }
        this.afterSelectionChange();
        this.syncCheckbox(checkbox, this.isProfileChecked(boxNode.box, profile.profileRange) ? 'checked' : 'none');
    }

    getHoldersText(node: IAssignmentNode) {
        if (!node.holders.length) return '—';
        const text = node.holders.map((holder) => `${holder.fullName ?? '—'} ${holder.documentCount}`).join(', ');
        const more = node.holderCount - node.holders.length;
        return more > 0 ? `${text} +${more}` : text;
    }

    getConflictText(conflict: IAssignmentConflict) {
        const node = `${getBoxLabel(conflict.box)} › ${getProfileLabel(conflict.profileRange)}`;
        const holder = conflict.holderFullName ?? '—';
        return conflict.isOwner ? `${node} — ${holder} đang phụ trách` : `${node} — ${holder} giữ ${conflict.documentCount} văn bản`;
    }

    runPreview() {
        if (!this.selection.size || this.previewing) return;
        this.previewing = true;
        this._assignmentService
            .preview(this.pair.projectId, this.pair.documentTypeId, this.buildRequest())
            .pipe(
                finalize(() => (this.previewing = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this.handleError(res, false);
                this.preview = res.data;
            });
    }

    assign() {
        if (!this.canAssign) return;
        this.assigning = true;
        this._assignmentService
            .assign(this.pair.projectId, this.pair.documentTypeId, this.buildRequest())
            .pipe(
                finalize(() => (this.assigning = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this.handleError(res, true);
                this.messageSuccess(
                    this.isQa
                        ? `Đã giao phụ trách cho ${this.memberName}`
                        : `Đã giao phiếu #${res.data.id} cho ${this.memberName} (${res.data.documentCount} văn bản)`,
                );
                this._ref.close(ASSIGN_DIALOG_RESULT.ASSIGNED);
            });
    }

    // Đang có tick → hỏi trước khi bỏ
    close() {
        if (!this.selection.size) {
            this._ref.close();
            return;
        }
        this._confirmationService.confirm({
            header: 'Bỏ các hộp / hồ sơ đã chọn?',
            message: `Đang chọn ${this.selection.size} nút, đóng là mất lựa chọn.`,
            closable: true,
            closeOnEscape: true,
            icon: 'pi pi-exclamation-triangle',
            rejectButtonProps: { label: 'Ở lại', severity: 'secondary', outlined: true },
            acceptButtonProps: { label: 'Bỏ chọn và đóng', severity: 'danger' },
            accept: () => this._ref.close(),
        });
    }

    // Toast câu BE đã hiện ở isResponseSucceed; ở đây chỉ quyết định làm gì tiếp
    private handleError(res: IBaseResponse, isAssigning: boolean) {
        const code = res.code;
        if (code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_PROJECT_CLOSED || code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_NOT_PROJECT_LEADER) {
            this._ref.close(ASSIGN_DIALOG_RESULT.RELOAD_PAIRS);
        } else if (code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_ASSIGNEE_NOT_MEMBER || code === ASSIGNMENT_ERROR_CODE.OPERATION_ERROR_LEADER_FORBIDDEN_TARGET) {
            this._ref.close(ASSIGN_DIALOG_RESULT.RELOAD_MEMBERS);
        } else if (isAssigning && RETRY_PREVIEW_CODES.includes(code)) {
            this.preview = null;
            this.runPreview();
        }
    }

    private buildRequest(): IAssignmentRequest {
        return {
            role: this.role,
            assigneeUserId: this.member.userId,
            items: [...this.selection.values()].map(({ box, profileRange }) => ({ box, profileRange })),
        };
    }

    private selectProfile(box: string, profile: IAssignmentNode) {
        const profileRange = profile.profileRange ?? '';
        this.selection.set(toProfileKey(box, profileRange), { box, profileRange, unassigned: profile.unassigned });
    }

    private getChildProfileKeys(box: string) {
        const prefix = toBoxKey(box) + KEY_SEPARATOR + PROFILE_ITEM_MARK;
        return [...this.selection.keys()].filter((key) => key.startsWith(prefix));
    }

    private afterSelectionChange() {
        this.preview = null;
        this._partialBoxes = new Set(
            [...this.selection.entries()].filter(([, item]) => item.profileRange !== null).map(([, item]) => toBoxKey(item.box)),
        );
        this.selectedUnassigned = [...this.selection.values()].reduce((sum, item) => sum + item.unassigned, 0);
    }

    // Ô tick gốc tự đảo khi bấm; tick bị chặn (vượt 1000) thì binding không đổi nên phải đặt lại DOM cho khớp
    private syncCheckbox(checkbox: HTMLInputElement, state: CheckState) {
        checkbox.checked = state === 'checked';
        checkbox.indeterminate = state === 'partial';
    }

    private warnMaxItems() {
        this.messageWarning(`Tối đa ${ASSIGNMENT_ITEMS_MAX} hộp / hồ sơ mỗi lần giao`);
    }

    private loadTree() {
        this.loadingTree = true;
        this._assignmentService
            .findTree(this.pair.projectId, this.pair.documentTypeId, {
                role: this.role,
                keyword: this.keyword.value.trim(),
                onlyUnassigned: this.onlyUnassigned,
                pageNumber: this.boxPage,
                pageSize: this.treePageSize,
            })
            .pipe(
                finalize(() => (this.loadingTree = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this.handleError(res, false);
                this.boxes = res.data.items;
                this.totalBoxes = res.data.totalItems;
            });
    }

    private loadProfiles(box: string) {
        const boxKey = toBoxKey(box);
        this.loadingProfiles.add(boxKey);
        this._assignmentService
            .findProfiles(this.pair.projectId, this.pair.documentTypeId, { role: this.role, box, pageNumber: 1, pageSize: PROFILE_PAGE_SIZE })
            .pipe(
                finalize(() => this.loadingProfiles.delete(boxKey)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    this.expandedBoxes.delete(boxKey);
                    return this.handleError(res, false);
                }
                this.profilesByBox.set(boxKey, res.data.items);
            });
    }
}
