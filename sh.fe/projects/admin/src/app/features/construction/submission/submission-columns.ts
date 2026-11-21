import { IListSubmission, ISubmissionColumn } from '@/models/submission.model';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn, ITableAction } from '@/shared/models/data-table.models';
import { SubmissionCheckStatusCellComponent, SubmissionQaStatusCellComponent } from './submission-status-cell/submission-status-cell.component';

// Dòng bảng Nhập liệu: thêm giá trị trường theo documentFieldId để cột động đọc bằng field 'fieldValues.<id>'
export type SubmissionRow = IListSubmission & { fieldValues: Record<number, string> };

export const SUBMISSION_ACTIONS = {
    CHECK: 'check',
    QA: 'qa',
    HISTORY: 'history',
} as const;

const CENTER_STYLE = 'text-align: center';
const DATE_TIME_FORMAT = 'dd/MM/yyyy HH:mm';

// STT · Tên file (bấm để mở) · Hồ sơ · Hộp — chung hai bảng
function leadingColumns(): IColumn[] {
    return [
        { header: 'STT', key: 'index', cellViewType: CellViewTypes.INDEX, locked: true, headerContainerStyle: 'width: 64px', cellStyle: CENTER_STYLE },
        { header: 'Tên file', field: 'fileName', clickable: true, locked: true },
        { header: 'Hồ sơ', field: 'profileRange' },
        { header: 'Hộp', field: 'box' },
    ];
}

function actionColumn(actions: ITableAction[]): IColumn {
    return {
        header: 'Thao tác',
        key: 'actions',
        cellViewType: CellViewTypes.ACTIONS,
        locked: true,
        frozen: 'right',
        headerContainerStyle: 'width: 56px; text-align: center',
        cellStyle: CENTER_STYLE,
        actions,
    };
}

// Bảng Nhập liệu (42.9). Cột động khoá để số cột động đổi không xoá cấu hình các cột cố định của tableKey
export function buildCheckColumns(fieldColumns: ISubmissionColumn[], isQaEnabled: boolean, actions: ITableAction[]): IColumn<SubmissionRow>[] {
    return [
        ...leadingColumns(),
        ...fieldColumns.map((column) => ({
            header: column.name,
            key: `field-${column.documentFieldId}`,
            field: `fieldValues.${column.documentFieldId}`,
            locked: true,
            headerContainerStyle: column.width ? `width: ${column.width}px` : undefined,
        })),
        { header: 'Kiểm', key: 'statusCheck', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: SubmissionCheckStatusCellComponent },
        ...(isQaEnabled ? [{ header: 'QA', key: 'statusQa', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: SubmissionQaStatusCellComponent }] : []),
        { header: 'Sửa lúc', field: 'modifiedDate', cellViewType: CellViewTypes.DATE, dateFormat: DATE_TIME_FORMAT },
        actionColumn(actions),
    ];
}

// Bảng QA (42.10): không có cột động vì qa/paging không trả dữ liệu trường
export function buildQaColumns(actions: ITableAction[]): IColumn[] {
    return [
        ...leadingColumns(),
        { header: 'Người kiểm', field: 'checkedUserName' },
        { header: 'Kiểm lúc', field: 'checkedDate', cellViewType: CellViewTypes.DATE, dateFormat: DATE_TIME_FORMAT },
        { header: 'QA', key: 'statusQa', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: SubmissionQaStatusCellComponent },
        { header: 'Người QA', field: 'qaUserName' },
        { header: 'QA lúc', field: 'qaDate', cellViewType: CellViewTypes.DATE, dateFormat: DATE_TIME_FORMAT },
        actionColumn(actions),
    ];
}
