import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { ShInputNumberComponent, ShSelectComponent } from '@sh/ui';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { finalize } from 'rxjs';
import { IUpdateMemberPriceItem, IUpdateProjectMember } from '@/models/project-member.model';
import { ProjectMemberService } from '@/services/project-member.service';
import { BaseComponent } from '@/shared/base/base.component';
import { MEMBER_PRICE_MAX, MEMBER_ROLE, MEMBER_ROLE_LABELS } from '@/shared/constants/project-member.constants';
import { SharedImports } from '@/shared/import.shared';
import { MANAGER_BY_ADMIN } from '../project-member-add/project-member-add.component';
import { formatMemberPrice, IProjectMemberDialogData, MemberAvatarComponent } from '../project-member-cells';

const LIST_PAGE_SIZE = 200;
const PRICE_NOT_CLEARABLE = 'Không xoá được giá đã đặt, chỉ sửa số';

@Component({
    selector: 'app-project-member-edit',
    imports: [SharedImports, ToggleSwitchModule, ShInputNumberComponent, ShSelectComponent, MemberAvatarComponent],
    templateUrl: './project-member-edit.component.html',
})
export class ProjectMemberEditComponent extends BaseComponent {
    private readonly _ref = inject(DynamicDialogRef);
    private readonly _data: IProjectMemberDialogData = inject(DynamicDialogConfig).data;
    private readonly _projectMemberService = inject(ProjectMemberService);
    private readonly _destroyRef = inject(DestroyRef);

    readonly member = this._data.member!;
    readonly isLeader = this.member.role === MEMBER_ROLE.LEADER;
    // Leader xem: giá công ty chỉ xem, chỉ sửa giá thực nhận (BE 809 nếu gửi giá công ty), không đổi Leader quản lý
    readonly isLeaderView = this._data.isLeaderView;
    readonly roleLabel = MEMBER_ROLE_LABELS[this.member.role] ?? this.member.role;
    readonly priceMax = MEMBER_PRICE_MAX;
    readonly companyPriceText = formatMemberPrice(this.member.companyPrice);
    readonly priceHint = this.isLeaderView
        ? `${this.member.role === MEMBER_ROLE.QA ? 'Mỗi lượt chấm' : 'Mỗi trường có dữ liệu'}. Giá mới áp cho cả phần đã làm.`
        : 'Theo mỗi trường có dữ liệu (Nhập liệu) / mỗi lượt chấm (QA). Thực nhận không vượt giá công ty.';

    // Giá đang lưu trên BE: lưu giá xong mà bước sau lỗi thì cập nhật để lần bấm sau chỉ gửi phần còn lại
    private _savedCompanyPrice = this.member.companyPrice;
    private _savedActualPrice = this.member.actualPrice;
    private readonly _savedManager = this.member.managedByUserId ?? MANAGER_BY_ADMIN;
    // Đã lưu được một phần (giá) thì đóng bằng Huỷ vẫn báo màn danh sách tải lại
    private _hasSaved = false;

    managers: { userId: string; label: string }[] = [{ userId: MANAGER_BY_ADMIN, label: 'Admin quản lý' }];
    submitting = false;

    override form = new FormGroup({
        companyPrice: new FormControl<number | null>({ value: this.member.companyPrice, disabled: this.isLeaderView }),
        // Leader chưa có giá công ty thì chưa đặt được giá thực nhận (BE 808)
        actualPrice: new FormControl<number | null>({ value: this.member.actualPrice, disabled: this.isLeaderView && this.member.companyPrice === null }),
        managedByUserId: new FormControl<string>(this._savedManager, { nonNullable: true }),
        isActive: new FormControl<boolean>(this.member.isActive, { nonNullable: true }),
    });

    get displayName() {
        return this.member.fullName || this.member.userName;
    }

    get companyPriceError() {
        const { companyPrice } = this.form.controls;
        return this._savedCompanyPrice !== null && companyPrice.value === null ? PRICE_NOT_CLEARABLE : null;
    }

    get actualPriceError() {
        const { companyPrice, actualPrice } = this.form.controls;
        if (this._savedActualPrice !== null && actualPrice.value === null) return PRICE_NOT_CLEARABLE;
        if (actualPrice.value !== null && companyPrice.value === null) return 'Cần giá công ty trước';
        if (actualPrice.value !== null && companyPrice.value !== null && actualPrice.value > companyPrice.value) return 'Không vượt giá công ty';
        return null;
    }

    get hasPriceError() {
        return !this.isLeader && !!(this.companyPriceError || this.actualPriceError);
    }

    override ngOnInit(): void {
        if (!this.isLeader && !this.isLeaderView) {
            this.loadManagers();
        }
    }

    close() {
        this._ref.close(this._hasSaved);
    }

    // Chỉ gửi phần đã đổi: (1) giá qua member/price, (2) Leader quản lý / Đang làm qua member/{id}; không đổi gì thì đóng
    onSubmit() {
        if (this.hasPriceError || this.submitting) return;
        const { companyPrice, actualPrice, managedByUserId, isActive } = this.form.getRawValue();

        const priceItem: IUpdateMemberPriceItem = { memberId: this.member.id };
        if (!this.isLeader && !this.isLeaderView && companyPrice !== this._savedCompanyPrice) priceItem.companyPrice = companyPrice!;
        if (!this.isLeader && actualPrice !== this._savedActualPrice) priceItem.actualPrice = actualPrice!;
        const isPriceChanged = priceItem.companyPrice !== undefined || priceItem.actualPrice !== undefined;

        const update: IUpdateProjectMember = { clearManager: false };
        if (!this.isLeader && !this.isLeaderView && managedByUserId !== this._savedManager) {
            if (managedByUserId === MANAGER_BY_ADMIN) {
                update.clearManager = true;
            } else {
                update.managedByUserId = managedByUserId;
            }
        }
        if (isActive !== this.member.isActive) update.isActive = isActive;
        const isMemberChanged = update.clearManager || update.managedByUserId !== undefined || update.isActive !== undefined;

        if (!isPriceChanged && !isMemberChanged) {
            this.close();
            return;
        }

        const { projectId } = this.member;
        const saveMember = () => {
            if (!isMemberChanged) return this.finish();
            this._projectMemberService
                .update(projectId, this.member.id, update)
                .pipe(
                    finalize(() => (this.submitting = false)),
                    takeUntilDestroyed(this._destroyRef),
                )
                .subscribe((res) => {
                    if (!this.isResponseSucceed(res)) return this._data.onMemberError(res.code);
                    this.finish();
                });
        };

        this.submitting = true;
        if (!isPriceChanged) {
            saveMember();
            return;
        }
        this._projectMemberService
            .updatePrices(projectId, { items: [priceItem] })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res)) {
                        this.submitting = false;
                        return this._data.onMemberError(res.code);
                    }
                    this._savedCompanyPrice = companyPrice;
                    this._savedActualPrice = actualPrice;
                    this._hasSaved = true;
                    saveMember();
                },
                error: () => (this.submitting = false),
            });
    }

    private finish() {
        this.submitting = false;
        this.messageSuccess(`Đã lưu ${this.displayName}`);
        this._ref.close(true);
    }

    // Leader đang hoạt động của cặp; "Admin quản lý" đứng đầu
    private loadManagers() {
        const { projectId, documentTypeId } = this.member;
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
