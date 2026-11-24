import { concatMap, from, last, takeWhile } from 'rxjs';
import { IProjectConfig } from '@/models/project-config.model';
import { ProjectConfigService } from '@/services/project-config.service';
import { PROJECT_CONFIG_ERROR_CODE, PROJECT_CONFIG_NAME } from '@/shared/constants/project-config.constants';
import { IBaseResponse } from '@/shared/models/request-paging.base.models';

// Dòng cấu hình màn dùng, tra theo Name
export type ProjectConfigMap = Partial<Record<string, IProjectConfig>>;

export interface IProjectConfigChange {
    name: string;
    value: string;
    // Chưa có dòng thì tạo mới
    row?: IProjectConfig;
}

const KNOWN_NAMES: string[] = Object.values(PROJECT_CONFIG_NAME);

// Bỏ dòng có Name lạ, màn không hiện (50.2)
export function toProjectConfigMap(rows: IProjectConfig[]): ProjectConfigMap {
    return Object.fromEntries(rows.filter((row) => KNOWN_NAMES.includes(row.name)).map((row) => [row.name, row]));
}

// Gửi tuần tự từng dòng đổi, dừng ở lỗi đầu và trả response cuối (50.7). Có id → PUT giữ description; chưa có → POST
export function saveProjectConfigs(service: ProjectConfigService, changes: IProjectConfigChange[]) {
    return from(changes).pipe(
        concatMap(({ name, value, row }) =>
            row ? service.update(row.id, { name, value, description: row.description }) : service.create({ name, value, description: '' }),
        ),
        takeWhile((res) => res.status === 1, true),
        last(),
    );
}

// 409 = người khác vừa tạo cùng dòng; lỗi khác (kể cả 304 dòng đã bị xoá) toast câu BE. Màn tải lại sau mọi lần lưu (50.10)
export function getSaveErrorMessage(res: IBaseResponse) {
    return res.code === PROJECT_CONFIG_ERROR_CODE.NAME_EXISTS ? 'Cấu hình vừa được người khác tạo, kiểm tra lại rồi lưu' : res.message;
}
