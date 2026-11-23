import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { ShMultiSelectComponent, ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { finalize, Observable } from 'rxjs';
import { IAddProjectMemberResult } from '@/models/project-member.model';
import { ProjectMemberService } from '@/services/project-member.service';
import { SharedService } from '@/services/shared.service';
import { StaffGroupService } from '@/services/staff-group.service';
import { BaseComponent } from '@/shared/base/base.component';
import { PermissionConstants } from '@/shared/constants/permission.constants';
import { MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';
import { IBaseResponseWithData } from '@/shared/models/request-paging.base.models';
import { SharedImports } from '@/shared/import.shared';
import { IProjectMemberDialogData } from '../project-member-cells';

// Giá trị "Admin quản lý" / "Không chọn" của ô chọn (= không gửi lên BE); id thật luôn là GUID / số dương
export const MANAGER_BY_ADMIN = 'admin';
const NO_STAFF_GROUP = 0;
const CANDIDATE_PAGE_SIZE = 50;
const LIST_PAGE_SIZE = 200;

const ROLE_BUTTONS = [MEMBER_ROLE.DATA, MEMBER_ROLE.QA, MEMBER_ROLE.LEADER].map((role) => ({ role, label: MEMBER_ROLE_LABELS[role] }));

type CandidateOption = { userId: string; label: string };

@Component({
    selector: 'app-project-member-add',
    imports: [SharedImports, ShMultiSelectComponent, ShSelectComponent],
    templateUrl: './project-member-add.component.html',
})
export class ProjectMemberAddComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IProjectMemberDialogData = inject(DynamicDialogConfig).data;
    private readonly _projectMemberService = inject(ProjectMemberService);
    private readonly _staffGroupService = inject(StaffGroupService);
    private readonly _sharedService = inject(SharedService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly pair = this._data.pair;
    readonly roleButtons = ROLE_BUTTONS;
    readonly qaRole = MEMBER_ROLE.QA;
    readonly canPickGroup = this._sharedService.isGranted(PermissionConstants.FUNCTION_STAFF_GROUP_MANAGE);

    role: string = MEMBER_ROLE.DATA;
    readonly userIds = new FormControl<string[]>([], { nonNullable: true });
    readonly staffGroupId = new FormControl<number>(NO_STAFF_GROUP, { nonNullable: true });
    readonly managedByUserId = new FormControl<string>(MANAGER_BY_ADMIN, { nonNullable: true });
    candidates: CandidateOption[] = [];
    staffGroups: { id: number; label: string }[] = [];
    managers: { userId: string; label: string }[] = [];
    submitting = false;
    // Nhãn của người đã chọn, giữ để chip vẫn hiện tên khi kết quả tìm mới không còn người đó
    private _selectedLabels = new Map<string, string>();

    get isLeaderRole() {
        return this.role === MEMBER_ROLE.LEADER;
    }

    get hasGroup() {
        return this.staffGroupId.value !== NO_STAFF_GROUP;
    }

    get canSubmit() {
        return this.userIds.value.length > 0 || (!this.isLeaderRole && this.hasGroup);
    }

    get submitLabel() {
        const count = this.userIds.value.length;
        return count && !(this.hasGroup && !this.isLeaderRole) ? `Thêm ${count} người` : 'Thêm';
    }

    override ngOnInit(): void {
        this.loadCandidates('');
        this.loadStaffGroups();
        this.loadManagers();
    }

    // Đổi vai trò → xoá người / nhóm đã chọn, tải lại gợi ý theo vai trò mới
    selectRole(role: string) {
        if (role === this.role || (role === MEMBER_ROLE.QA && !this.pair.isQaEnabled)) return;
        this.role = role;
        this.userIds.setValue([]);
        this._selectedLabels.clear();
        this.staffGroupId.setValue(NO_STAFF_GROUP);
        this.loadCandidates('');
        this.loadStaffGroups();
    }

    onCandidateFilter(event: { filter?: string }) {
        this.loadCandidates(event.filter?.trim() ?? '');
    }

    onUsersChange(userIds: string[]) {
        this.candidates.filter((item) => userIds.includes(item.userId)).forEach((item) => this._selectedLabels.set(item.userId, item.label));
    }

    close() {
        this._ref.close(false);
    }

    onSubmit() {
        if (!this.canSubmit) return;
        const { projectId, documentTypeId } = this.pair;
        const userIds = this.userIds.value;
        const request: Observable<IBaseResponseWithData<IAddProjectMemberResult>> = this.isLeaderRole
            ? this._projectMemberService.addLeaders(projectId, { documentTypeId, userIds })
            : this._projectMemberService.addMembers(projectId, {
                  documentTypeId,
                  managedByUserId: this.managedByUserId.value === MANAGER_BY_ADMIN ? undefined : this.managedByUserId.value,
                  users: userIds.map((userId) => ({ userId, role: this.role })),
                  staffGroupIds: this.hasGroup ? [this.staffGroupId.value] : [],
              });

        this.submitting = true;
        request
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this._data.onMemberError(res.code);
                const { addedCount, skipped } = res.data;
                if (addedCount > 0) {
                    this.messageSuccess(`Đã thêm ${addedCount} người`);
                }
                if (skipped.length) {
                    this._messageService.add({
                        severity: 'warn',
                        sticky: true,
                        closable: true,
                        summary: `Bỏ qua ${skipped.length} người:`,
                        detail: skipped.map((item) => `${item.fullName || item.userId}: ${item.reason}`).join('\n'),
                        styleClass: 'whitespace-pre-line',
                    });
                }
                this._ref.close(true);
            });
    }

    // Gợi ý người có vai trò đang chọn và chưa ở cặp; luôn giữ người đã chọn trong danh sách
    private loadCandidates(keyword: string) {
        const { projectId, documentTypeId } = this.pair;
        const role = this.role;
        this._projectMemberService
            .findCandidates(projectId, { documentTypeId, role, keyword, pageNumber: 1, pageSize: CANDIDATE_PAGE_SIZE })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (role !== this.role) return;
                if (!this.isResponseSucceed(res)) return this._data.onMemberError(res.code);
                const found = res.data.items.map((item) => ({ userId: item.userId, label: `${item.fullName || item.userName} (${item.userName})` }));
                const selected = this.userIds.value
                    .filter((userId) => !found.some((item) => item.userId === userId))
                    .map((userId) => ({ userId, label: this._selectedLabels.get(userId) ?? userId }));
                this.candidates = [...selected, ...found];
            });
    }

    // Nhóm đúng vai trò đang chọn (BE tự giới hạn Leader chỉ nhóm mình); Leader không thêm theo nhóm
    private loadStaffGroups() {
        this.staffGroups = [{ id: NO_STAFF_GROUP, label: 'Không chọn' }];
        if (!this.canPickGroup || this.isLeaderRole) return;
        const role = this.role;
        this._staffGroupService
            .findPaging({ role, pageNumber: 1, pageSize: LIST_PAGE_SIZE })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (role !== this.role || !this.isResponseSucceed(res)) return;
                this.staffGroups = [
                    { id: NO_STAFF_GROUP, label: 'Không chọn' },
                    ...res.data.items.map((group) => ({ id: group.id, label: `${group.name} (${group.memberCount} người)` })),
                ];
            });
    }

    // Leader đang hoạt động của cặp; "Admin quản lý" đứng đầu
    private loadManagers() {
        const { projectId, documentTypeId } = this.pair;
        this.managers = [{ userId: MANAGER_BY_ADMIN, label: 'Admin quản lý' }];
        this._projectMemberService
            .findPaging(projectId, { documentTypeId, role: MEMBER_ROLE.LEADER, isActive: true, pageNumber: 1, pageSize: LIST_PAGE_SIZE })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) return this._data.onMemberError(res.code);
                this.managers = [
                    { userId: MANAGER_BY_ADMIN, label: 'Admin quản lý' },
                    ...res.data.items.map((leader) => ({ userId: leader.userId, label: leader.fullName || leader.userName })),
                ];
            });
    }
}
