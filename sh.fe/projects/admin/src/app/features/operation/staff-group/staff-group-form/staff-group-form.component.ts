import { formatDate } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { ShInputTextComponent } from '@sh/ui';
import { AutoCompleteCompleteEvent, AutoCompleteModule, AutoCompleteSelectEvent } from 'primeng/autocomplete';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { Observable } from 'rxjs';
import { IStaffGroupCandidate, IStaffGroupDetail } from '@/models/staff-group.model';
import { StaffGroupService } from '@/services/staff-group.service';
import { BaseComponent } from '@/shared/base/base.component';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';
import {
    STAFF_GROUP_CANDIDATE_PAGE_SIZE,
    STAFF_GROUP_DESCRIPTION_MAX,
    STAFF_GROUP_ERROR_CODE,
    STAFF_GROUP_MEMBERS_MAX,
    STAFF_GROUP_NAME_MAX,
} from '@/shared/constants/staff-group.constants';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';
import { SharedImports } from '@/shared/import.shared';
import { MEMBER_ROLE_STYLES, MemberAvatarComponent } from '../../project-member/project-member-cells';
import { IStaffGroupDialogData, MISMATCH_ROLE_STYLE, STAFF_GROUP_ROLES } from '../staff-group-cells';

const NAME_INVALID = `Tên nhóm 1–${STAFF_GROUP_NAME_MAX} ký tự`;
const NAME_EXISTS = 'Bạn đã có nhóm tên này';
const INVALID_REQUEST = 'Dữ liệu gửi lên không hợp lệ';

// staffRole: vai trò Data / QA hiện tại của người đó (null = không còn vai trò nào trong hai vai trò này)
interface IFormMember {
    userId: string;
    userName: string;
    fullName: string;
    staffRole: string | null;
}

type CandidateOption = IStaffGroupCandidate & { label: string };

@Component({
    selector: 'app-staff-group-form',
    imports: [SharedImports, AutoCompleteModule, ShInputTextComponent, MemberAvatarComponent],
    templateUrl: './staff-group-form.component.html',
})
export class StaffGroupFormComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IStaffGroupDialogData = inject(DynamicDialogConfig).data;
    private readonly _staffGroupService = inject(StaffGroupService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly group = this._data.group;
    readonly isEdit = !!this.group;
    readonly nameMax = STAFF_GROUP_NAME_MAX;
    readonly descriptionMax = STAFF_GROUP_DESCRIPTION_MAX;
    readonly roleButtons = [MEMBER_ROLE.DATA, MEMBER_ROLE.QA].map((role) => ({ role, label: MEMBER_ROLE_LABELS[role] }));

    detail: IStaffGroupDetail | null = null;
    loadingDetail = this.isEdit;
    submitting = false;
    submitted = false;
    // Vai trò đang chọn: dialog Tạo / nhóm rỗng chọn được; nhóm đã có người thì khoá theo vai trò lúc mở
    role: string | null = MEMBER_ROLE.DATA;
    roleLocked = false;
    members: IFormMember[] = [];
    search: CandidateOption | string | null = null;
    suggestions: CandidateOption[] = [];
    // 802 từ BE, xoá khi sửa tên
    nameExistsError = false;

    // Phần đã lưu trên BE: bước giữa lỗi thì lần bấm sau chỉ gửi phần còn lại
    private _savedName = '';
    private _savedDescription: string | null = null;
    private _savedUserIds = new Set<string>();
    private _hasSaved = false;

    override form = new FormGroup({
        name: new FormControl('', { nonNullable: true }),
        description: new FormControl('', { nonNullable: true }),
    });

    get subtitle() {
        if (!this.isEdit) return 'Bạn là trưởng nhóm';
        return `Trưởng nhóm: ${this.group!.ownerFullName ?? '—'} · tạo ${this.toDateText(this.group!.createdDate)}`;
    }

    get nameError() {
        if (this.nameExistsError) return NAME_EXISTS;
        const { name } = this.form.controls;
        return (name.touched || this.submitted) && !name.value.trim() ? NAME_INVALID : null;
    }

    // Nhóm lệch: thành viên còn lại có hơn một vai trò
    get isMixed() {
        return new Set(this.members.map((member) => member.staffRole)).size > 1;
    }

    // Vai trò nhóm: khoá → suy từ thành viên (lệch thì lấy bên đông hơn, bằng nhau = không chọn); bỏ hết người vẫn giữ vai trò cũ
    get currentRole(): string | null {
        if (!this.roleLocked || !this.members.length) return this.role;
        if (!this.isMixed) return this.members[0].staffRole;
        const dataCount = this.members.filter((member) => member.staffRole === MEMBER_ROLE.DATA).length;
        const qaCount = this.members.filter((member) => member.staffRole === MEMBER_ROLE.QA).length;
        if (dataCount === qaCount) return null;
        return dataCount > qaCount ? MEMBER_ROLE.DATA : MEMBER_ROLE.QA;
    }

    // Nhóm lệch hoà mà bỏ hết người thì không còn vai trò để khoá → mở cho chọn
    get isRoleLocked() {
        return this.roleLocked && !!this.currentRole;
    }

    get roleHint() {
        return this.isRoleLocked
            ? 'Nhóm đã có người nên không đổi vai trò. Chỉ gợi ý người cùng vai trò.'
            : 'Chỉ gợi ý người có vai trò này. Một nhóm chỉ gồm Nhập liệu hoặc chỉ gồm QA.';
    }

    // BE trả 804 khi thêm người vào nhóm còn lệch: khoá ô cho tới khi chỉ còn một vai trò
    get canAddMember() {
        return !this.loadingDetail && !this.isMixed && !!this.currentRole;
    }

    get addedUserIds() {
        return this.members.filter((member) => !this._savedUserIds.has(member.userId)).map((member) => member.userId);
    }

    get removedUserIds() {
        const memberIds = new Set(this.members.map((member) => member.userId));
        return [...this._savedUserIds].filter((userId) => !memberIds.has(userId));
    }

    get membersError() {
        const isTooMany = this.addedUserIds.length > STAFF_GROUP_MEMBERS_MAX || this.removedUserIds.length > STAFF_GROUP_MEMBERS_MAX;
        return isTooMany ? `Tối đa ${STAFF_GROUP_MEMBERS_MAX} người mỗi lần` : null;
    }

    override ngOnInit(): void {
        this.form.controls.name.valueChanges.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => (this.nameExistsError = false));
        if (this.isEdit) {
            this.loadDetail();
        }
    }

    selectRole(role: string) {
        if (this.isRoleLocked || role === this.role) return;
        this.role = role;
        // Đổi vai trò → bỏ hết người đã chọn (không hỏi); chỉ xảy ra khi chưa có ai đã lưu
        this.members = [];
        this.suggestions = [];
    }

    // Nhãn chip trên dòng thành viên: chỉ khi nhóm lệch
    getMemberChip(member: IFormMember) {
        if (!this.isMixed) return null;
        const roleLabel = member.staffRole ? (MEMBER_ROLE_LABELS[member.staffRole] ?? member.staffRole) : 'vai trò khác';
        const currentRole = this.currentRole;
        if (!currentRole) {
            return { label: roleLabel, style: MEMBER_ROLE_STYLES[member.staffRole ?? ''] ?? MISMATCH_ROLE_STYLE };
        }
        return member.staffRole === currentRole ? null : { label: `Đang là ${roleLabel} — lệch vai trò`, style: MISMATCH_ROLE_STYLE };
    }

    searchCandidates(event: AutoCompleteCompleteEvent) {
        const role = this.currentRole;
        if (!role) return;
        this._staffGroupService
            .candidates({
                role,
                keyword: event.query.trim(),
                excludeStaffGroupId: this.group?.id,
                pageNumber: 1,
                pageSize: STAFF_GROUP_CANDIDATE_PAGE_SIZE,
            })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return;
                const memberIds = new Set(this.members.map((member) => member.userId));
                this.suggestions = res.data.items
                    .filter((candidate) => !memberIds.has(candidate.userId))
                    .map((candidate) => ({ ...candidate, label: `${candidate.fullName} (${candidate.userName})` }));
            });
    }

    addMember(event: AutoCompleteSelectEvent) {
        const candidate = event.value as CandidateOption;
        if (!this.members.some((member) => member.userId === candidate.userId)) {
            this.members = [
                ...this.members,
                { userId: candidate.userId, userName: candidate.userName, fullName: candidate.fullName, staffRole: candidate.role },
            ];
        }
        this.search = null;
    }

    removeMember(member: IFormMember) {
        this.members = this.members.filter((item) => item.userId !== member.userId);
    }

    close() {
        this._ref.close(this._hasSaved);
    }

    onSubmit() {
        this.submitted = true;
        this.form.markAllAsTouched();
        if (this.nameError || this.membersError || this.submitting || this.loadingDetail) return;
        if (this.isEdit) {
            this.saveEdit();
        } else {
            this.create();
        }
    }

    private create() {
        const { name, description } = this.getFormValue();
        this.submitting = true;
        this._staffGroupService
            .create({ name, description, userIds: this.members.map((member) => member.userId) })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    this.submitting = false;
                    if (!this.handleResult(res)) return;
                    this.messageSuccess(`Đã tạo nhóm ${name}`);
                    this._ref.close(true);
                },
                error: (error) => this.onHttpError(error),
            });
    }

    // Chỉ gửi phần đổi, theo thứ tự: (1) tên / mô tả, (2) bỏ người, (3) thêm người — bỏ trước để gỡ người lệch rồi mới thêm
    private saveEdit() {
        const id = this.group!.id;
        const { name, description } = this.getFormValue();
        const removedUserIds = this.removedUserIds;
        const addedUserIds = this.addedUserIds;
        const steps: { request: () => Observable<IBaseResponse>; onSaved: () => void }[] = [];

        if (name !== this._savedName || description !== this._savedDescription) {
            steps.push({
                request: () => this._staffGroupService.update(id, { name, description }),
                onSaved: () => {
                    this._savedName = name;
                    this._savedDescription = description;
                },
            });
        }
        if (removedUserIds.length) {
            steps.push({
                request: () => this._staffGroupService.removeMembers(id, { userIds: removedUserIds }),
                onSaved: () => removedUserIds.forEach((userId) => this._savedUserIds.delete(userId)),
            });
        }
        if (addedUserIds.length) {
            steps.push({
                request: () => this._staffGroupService.addMembers(id, { userIds: addedUserIds }),
                onSaved: () => addedUserIds.forEach((userId) => this._savedUserIds.add(userId)),
            });
        }

        if (!steps.length) {
            this.close();
            return;
        }
        this.submitting = true;
        const runStep = (index: number) => {
            if (index === steps.length) {
                this.submitting = false;
                this.messageSuccess(`Đã lưu nhóm ${name}`);
                this._ref.close(true);
                return;
            }
            steps[index]
                .request()
                .pipe(takeUntilDestroyed(this._destroyRef))
                .subscribe({
                    next: (res) => {
                        if (!this.handleResult(res)) {
                            this.submitting = false;
                            return;
                        }
                        steps[index].onSaved();
                        this._hasSaved = true;
                        runStep(index + 1);
                    },
                    error: (error) => this.onHttpError(error),
                });
        };
        runStep(0);
    }

    // 802 → lỗi tại ô Tên; 801 → nhóm không còn / không thuộc mình: toast + đóng + tải lại bảng; còn lại toast câu BE, giữ dialog
    private handleResult(res: IBaseResponse) {
        if (res.status === 1) return true;
        if (res.code === STAFF_GROUP_ERROR_CODE.OPERATION_ERROR_STAFF_GROUP_NAME_EXISTS) {
            this.nameExistsError = true;
            return false;
        }
        this.isResponseSucceed(res);
        if (res.code === STAFF_GROUP_ERROR_CODE.OPERATION_ERROR_STAFF_GROUP_NOT_FOUND) {
            this._ref.close(true);
        }
        return false;
    }

    // API ghi tắt toast chung của interceptor: HTTP 400 (sai định dạng DTO) báo câu riêng
    private onHttpError(error: HttpErrorResponse) {
        this.submitting = false;
        this.messageError(error.status === 400 ? INVALID_REQUEST : '');
    }

    private getFormValue() {
        const { name, description } = this.form.getRawValue();
        return { name: name.trim(), description: description.trim() || null };
    }

    private loadDetail() {
        this._staffGroupService
            .findById(this.group!.id)
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.handleResult(res)) return;
                const detail = res.data;
                this.detail = detail;
                this.form.setValue({ name: detail.name, description: detail.description ?? '' }, { emitEvent: false });
                this._savedName = detail.name;
                this._savedDescription = detail.description;
                this.members = detail.members.map((member) => ({
                    userId: member.userId,
                    userName: member.userName,
                    fullName: member.fullName,
                    staffRole: member.roleNames.split(', ').find((roleName) => STAFF_GROUP_ROLES.includes(roleName)) ?? null,
                }));
                this._savedUserIds = new Set(this.members.map((member) => member.userId));
                // Nhóm rỗng mở như dialog Tạo; có người (kể cả lệch) thì khoá, vai trò suy từ thành viên
                this.roleLocked = detail.role !== null;
                this.role = this.roleLocked ? this.currentRole : MEMBER_ROLE.DATA;
                this.loadingDetail = false;
            });
    }

    private toDateText(value: string | null) {
        return value ? formatDate(value, 'dd/MM/yyyy', 'en-US') : '—';
    }
}
