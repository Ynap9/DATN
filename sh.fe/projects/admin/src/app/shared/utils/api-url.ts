import { ApiVersionConstants } from '@/shared/constants/api-version.constants';

export function apiUrl(path: string, version: string = ApiVersionConstants.DEFAULT_VERSION): string {
    return `/api/${version}/${path}`;
}
