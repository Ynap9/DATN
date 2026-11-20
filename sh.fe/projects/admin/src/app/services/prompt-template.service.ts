import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { IComposedPrompt, IPromptTemplateVersion, IPublishPromptTemplate, ISavePromptDraft, IViewPromptEditor } from "@/models/prompt-template.model";
import { IBaseResponse, IBaseResponseWithData } from "@/shared/models/request-paging.base.models";
import { apiUrl } from "@/shared/utils/api-url";

@Injectable({
  providedIn: 'root'
})
export class PromptTemplateService {
    api = apiUrl('ai/prompt-template');
    http = inject(HttpClient);

    findEditor(documentTypeId: number) {
        return this.http.get<IBaseResponseWithData<IViewPromptEditor>>(`${this.api}/document-type/${documentTypeId}`);
    }

    findVersions(documentTypeId: number) {
        return this.http.get<IBaseResponseWithData<IPromptTemplateVersion[]>>(`${this.api}/document-type/${documentTypeId}/versions`);
    }

    // Trả id bản nháp (tạo mới nếu metadata chưa có nháp)
    saveDraft(documentTypeId: number, body: ISavePromptDraft) {
        return this.http.put<IBaseResponseWithData<number>>(`${this.api}/document-type/${documentTypeId}/draft`, body);
    }

    publish(id: number, body: IPublishPromptTemplate) {
        return this.http.post<IBaseResponse>(`${this.api}/${id}/publish`, body);
    }

    // Trả id bản nháp mới chép từ bản được khôi phục
    restore(id: number) {
        return this.http.post<IBaseResponseWithData<number>>(`${this.api}/${id}/restore`, {});
    }

    discard(id: number) {
        return this.http.delete<IBaseResponse>(`${this.api}/${id}`);
    }

    preview(id: number) {
        return this.http.get<IBaseResponseWithData<IComposedPrompt>>(`${this.api}/${id}/preview`);
    }
}
