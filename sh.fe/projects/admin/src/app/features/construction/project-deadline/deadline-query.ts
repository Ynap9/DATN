import { ActivatedRoute, ParamMap, Params, Router } from '@angular/router';
import { IDeadlineProjectFilter } from '@/models/project-deadline.model';
import {
    DEADLINE_RESULT_FILTER,
    DEADLINE_TAB,
    DEADLINE_TYPE,
    DEADLINE_TYPE_ALL,
    DeadlineResultFilter,
    DeadlineTab,
} from '@/shared/constants/project-deadline.constants';

// Tên tham số trên query string màn Hạn dự án; projectName chỉ để hiện chip, không nằm trong bộ lọc BE
export const DEADLINE_QUERY = {
    TAB: 'tab',
    TYPE: 'type',
    SUCCESS: 'success',
    FROM: 'from',
    TO: 'to',
    KEYWORD: 'keyword',
    PROJECT: 'project',
    PROJECT_NAME: 'projectName',
} as const;

export interface IDeadlineQuery {
    tab: DeadlineTab;
    type: number;
    result: DeadlineResultFilter;
    from: string | null;
    to: string | null;
    keyword: string;
    project: IDeadlineProjectFilter | null;
}

const DATE_PARAM = /^\d{4}-\d{2}-\d{2}$/;
const TYPES: number[] = [DEADLINE_TYPE.SOON, DEADLINE_TYPE.EXPIRED];
const RESULT_BY_SUCCESS_PARAM: Record<string, DeadlineResultFilter> = {
    true: DEADLINE_RESULT_FILTER.SUCCESS,
    false: DEADLINE_RESULT_FILTER.FAILED,
};

// Đọc query string theo kiểu từng tham số; giá trị lạ về mặc định (Tất cả / trống)
export function readDeadlineQuery(params: ParamMap): IDeadlineQuery {
    const type = Number(params.get(DEADLINE_QUERY.TYPE));
    const from = params.get(DEADLINE_QUERY.FROM);
    const to = params.get(DEADLINE_QUERY.TO);
    const projectId = Number(params.get(DEADLINE_QUERY.PROJECT));
    return {
        tab: params.get(DEADLINE_QUERY.TAB) === DEADLINE_TAB.MAILS ? DEADLINE_TAB.MAILS : DEADLINE_TAB.ATTENTION,
        type: TYPES.includes(type) ? type : DEADLINE_TYPE_ALL,
        result: RESULT_BY_SUCCESS_PARAM[params.get(DEADLINE_QUERY.SUCCESS) ?? ''] ?? DEADLINE_RESULT_FILTER.ALL,
        from: from && DATE_PARAM.test(from) ? from : null,
        to: to && DATE_PARAM.test(to) ? to : null,
        keyword: params.get(DEADLINE_QUERY.KEYWORD)?.trim() ?? '',
        project: Number.isInteger(projectId) && projectId > 0 ? { projectId, name: params.get(DEADLINE_QUERY.PROJECT_NAME) || `#${projectId}` } : null,
    };
}

// Giá trị lọc kết quả -> tham số success của query string (Tất cả = bỏ tham số)
export function toSuccessParam(result: DeadlineResultFilter) {
    if (result === DEADLINE_RESULT_FILTER.ALL) return null;
    return String(result === DEADLINE_RESULT_FILTER.SUCCESS);
}

// Ghi đè từng tham số vào URL hiện tại, giữ tham số khác; null / chuỗi rỗng thì xoá. Không thêm mục lịch sử trình duyệt
export function updateDeadlineQuery(router: Router, route: ActivatedRoute, change: Params) {
    const queryParams = Object.fromEntries(Object.entries(change).map(([key, value]) => [key, value === '' ? null : value]));
    router.navigate([], { relativeTo: route, queryParams, queryParamsHandling: 'merge', replaceUrl: true });
}
