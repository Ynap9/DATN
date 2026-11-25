import { Component, computed, input } from '@angular/core';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { IViewWageSummary } from '@/models/wage.model';
import { MEMBER_ROLE } from '@/shared/constants/project-member.constants';
import { WAGE_VIEW_MODE, WageViewMode } from '@/shared/constants/wage.constants';
import { formatWageNumber, WageRow } from '../cells/wage-row';

interface ISummaryTile {
    label: string;
    // null = chưa đủ dòng để cộng (hiện "—" + tooltip)
    value: number | null;
    unit: string;
    note: string;
}

const PARTIAL_TOOLTIP = 'Xem đủ các trang để có tổng';

// Admin / Leader: 4 ô lấy từ summary (tổng cả bộ lọc). Nhân viên: 3 ô, phần cộng từ dòng chỉ đúng khi mọi dòng nằm trong trang
@Component({
    selector: 'app-wage-summary',
    imports: [SkeletonModule, TooltipModule],
    templateUrl: './wage-summary.component.html',
})
export class WageSummaryComponent {
    viewMode = input.required<WageViewMode>();
    staffRole = input.required<string>();
    summary = input<IViewWageSummary | null>(null);
    rows = input<WageRow[]>([]);
    total = input(0);
    loading = input(false);

    readonly partialTooltip = PARTIAL_TOOLTIP;
    readonly format = formatWageNumber;
    readonly isStaff = computed(() => this.viewMode() === WAGE_VIEW_MODE.STAFF);
    readonly isAllRowsLoaded = computed(() => this.total() <= this.rows().length);

    readonly tiles = computed<ISummaryTile[]>(() => {
        const summary = this.summary();
        const isAdmin = this.viewMode() === WAGE_VIEW_MODE.ADMIN;
        if (!this.isStaff()) {
            return [
                { label: 'Tiền thực nhận', value: summary?.actualAmount ?? 0, unit: 'đ', note: isAdmin ? 'Phải trả nhân viên' : 'Bạn trả nhân viên' },
                { label: 'Theo giá công ty', value: summary?.companyAmount ?? null, unit: 'đ', note: isAdmin ? 'Admin trả Leader' : 'Công ty trả bạn' },
                { label: 'Nhập liệu', value: summary?.dataQuantity ?? 0, unit: 'trường', note: 'có dữ liệu, tính tiền' },
                { label: 'QA', value: summary?.qaQuantity ?? 0, unit: 'lượt', note: 'lượt chấm' },
            ];
        }
        const rows = this.rows();
        const isComplete = this.isAllRowsLoaded();
        const sum = (pick: (row: WageRow) => number) => (isComplete ? rows.reduce((total, row) => total + pick(row), 0) : null);
        const metadataNote = `${this.total()} metadata`;
        if (this.staffRole() === MEMBER_ROLE.QA) {
            const missingCount = isComplete ? rows.filter((row) => row.isPriceMissing).length : 0;
            const passed = sum((row) => row.passedCount);
            const failed = sum((row) => row.failedCount);
            return [
                { label: 'Tiền tạm tính', value: summary?.actualAmount ?? 0, unit: 'đ', note: missingCount ? `${missingCount} metadata chưa có đơn giá` : metadataNote },
                { label: 'Văn bản đã chấm', value: sum((row) => row.reviewDocumentCount), unit: 'văn bản', note: 'khác nhau' },
                {
                    label: 'Khối lượng',
                    value: summary?.qaQuantity ?? 0,
                    unit: 'lượt',
                    note: passed === null || failed === null ? '' : `${this.format(passed)} Đạt · ${this.format(failed)} Không đạt`,
                },
            ];
        }
        return [
            { label: 'Tiền tạm tính', value: summary?.actualAmount ?? 0, unit: 'đ', note: metadataNote },
            { label: 'Văn bản đã kiểm xong', value: sum((row) => row.documentCount), unit: 'văn bản', note: 'đang do bạn giữ' },
            { label: 'Khối lượng', value: summary?.dataQuantity ?? 0, unit: 'trường', note: 'có dữ liệu, tính tiền' },
        ];
    });
}
