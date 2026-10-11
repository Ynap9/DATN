export interface IEnvironment {
    production: boolean;
    api: string;
}

export const environment: IEnvironment = {
    production: false,
    api: 'http://localhost:5265',
};
