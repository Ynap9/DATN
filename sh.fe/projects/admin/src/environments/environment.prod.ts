import { IEnvironment } from '@/shared/models/environment.models';

export const environment: IEnvironment = {
    production: false,
    baseUrl: 'http://localhost:5265',
    authGrantType: 'password',
    authClientId: 'client-web2',
    authScope: 'openid offline_access',
    appUrl: 'http://localhost:4200',
};
