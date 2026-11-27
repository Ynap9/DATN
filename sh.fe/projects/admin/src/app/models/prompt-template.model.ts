export interface IPromptTemplateInfo {
    id: number;
    version: number;
    status: string;
    note?: string | null;
    sourceVersion?: number | null;
    modifiedByName?: string | null;
    modifiedDate?: string | null;
    publishedByName?: string | null;
    publishedDate?: string | null;
}

export interface IPromptFieldEditor {
    documentFieldId: number;
    name: string;
    outputKey?: string | null;
    description: string;
    example?: string | null;
    normalizeRule: string;
    isRequired: boolean;
    isActive: boolean;
    inTemplate: boolean;
}

export interface IViewPromptEditor {
    documentTypeId: number;
    documentTypeName: string;
    published?: IPromptTemplateInfo | null;
    draft?: IPromptTemplateInfo | null;
    editingId?: number | null;
    systemPrompt: string;
    instructionPrompt: string;
    fields: IPromptFieldEditor[];
    missingFields: string[];
    invalidFields: string[];
}

export interface ISavePromptField {
    documentFieldId: number;
    description: string;
    example?: string | null;
    normalizeRule: string;
    isRequired: boolean;
    isActive: boolean;
}

export interface ISavePromptDraft {
    systemPrompt: string;
    instructionPrompt: string;
    note?: string | null;
    draftModifiedDate?: string | null;
    fields: ISavePromptField[];
}

export interface IPublishPromptTemplate {
    note?: string | null;
    draftModifiedDate?: string | null;
}

export interface IPromptTemplateVersion {
    id: number;
    version: number;
    status: string;
    note?: string | null;
    sourceVersion?: number | null;
    createdByName?: string | null;
    createdDate?: string | null;
    publishedByName?: string | null;
    publishedDate?: string | null;
}

export interface IComposedPrompt {
    systemPrompt: string;
    prompt: string;
}
