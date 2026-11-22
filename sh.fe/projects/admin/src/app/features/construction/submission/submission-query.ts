import { ParamMap } from '@angular/router';
import { ISubmissionFilter } from '@/models/submission.model';
import { SubmissionView } from '@/shared/constants/submission.constants';

// Trạng thái lọc trên query string của màn danh sách văn bản / Nhập liệu (đặt đúng tên tham số BE)
export type SubmissionQuery = Partial<Omit<ISubmissionFilter, 'projectId' | 'documentTypeId'>> & {
    pageNumber?: number;
    pageSize?: number;
    view?: SubmissionView;
};

const NUMBER_PARAMS = ['statusCheck', 'statusQa', 'documentFieldId1', 'documentFieldId2', 'pageNumber', 'pageSize'];
const BOOLEAN_PARAMS = ['isEmpty1', 'isEmpty2'];
const STRING_PARAMS = ['keyword', 'fieldValue1', 'fieldValue2', 'view'];

// Đọc query string theo kiểu từng tham số; bỏ tham số lạ và số không hợp lệ
export function readSubmissionQuery(params: ParamMap): SubmissionQuery {
    const query: Record<string, string | number | boolean> = {};
    params.keys.forEach((key) => {
        const value = params.get(key)!;
        if (NUMBER_PARAMS.includes(key) && value !== '' && !isNaN(Number(value))) {
            query[key] = Number(value);
        } else if (BOOLEAN_PARAMS.includes(key)) {
            query[key] = value === 'true';
        } else if (STRING_PARAMS.includes(key)) {
            query[key] = value;
        }
    });
    return query;
}
