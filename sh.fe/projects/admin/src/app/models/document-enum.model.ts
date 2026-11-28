export interface IDocumentEnumField {
    id: number;
    name: string;
}

export interface IViewDocumentEnum {
    id: number;
    name: string;
    values: string[];
    usedByFields: IDocumentEnumField[];
}

export interface ISaveDocumentEnum {
    name: string;
    values: string[];
}
