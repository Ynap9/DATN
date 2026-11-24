export interface IProjectConfig {
    id: number;
    name: string;
    value: string;
    description: string;
    createdDate: string | null;
    modifiedDate: string | null;
}

export interface ISaveProjectConfig {
    name: string;
    value: string;
    description: string;
}
