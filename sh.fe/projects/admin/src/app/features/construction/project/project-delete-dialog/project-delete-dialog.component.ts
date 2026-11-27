import { Component, DestroyRef, inject, input, model, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { finalize } from 'rxjs';
import { IDocumentTypeDropdown } from '@/models/document-type.model';
import { ProjectService } from '@/services/project.service';
import { BaseComponent } from '@/shared/base/base.component';
import { SharedImports } from '@/shared/import.shared';

// Khớp tên: bỏ khoảng trắng hai đầu, không phân biệt hoa thường, giữ dấu tiếng Việt
function normalizeName(name: string) {
    return name.trim().toLowerCase();
}

// Xoá cả dự án phải gõ lại đúng tên dự án mới bật nút Xóa
@Component({
    selector: 'app-project-delete-dialog',
    imports: [SharedImports, DialogModule, MessageModule, TagModule],
    templateUrl: './project-delete-dialog.component.html',
})
export class ProjectDeleteDialogComponent extends BaseComponent {
    private readonly _projectService = inject(ProjectService);
    private readonly _destroyRef = inject(DestroyRef);

    projectId = input.required<number>();
    projectName = input.required<string>();
    documentTypes = input.required<IDocumentTypeDropdown[]>();
    visible = model(false);

    deleted = output<void>();

    typedName = '';
    documentTypeNames: string[] = [];
    deleting = false;

    get isNameMatched() {
        return normalizeName(this.typedName) === normalizeName(this.projectName());
    }

    // Metadata lấy theo Id dự án vì danh sách phân trang / có lọc không đủ mọi dòng của dự án
    loadDocumentTypeNames() {
        this._projectService
            .findById(this.projectId())
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!this.isResponseSucceed(res)) {
                    this.close();
                    return;
                }
                this.documentTypeNames = this.documentTypes()
                    .filter((documentType) => res.data.documentTypeIds.includes(documentType.id))
                    .map((documentType) => documentType.name);
            });
    }

    reset() {
        this.typedName = '';
        this.documentTypeNames = [];
    }

    close() {
        this.visible.set(false);
    }

    confirm() {
        if (!this.isNameMatched || this.deleting) return;

        this.deleting = true;
        this._projectService
            .deleteCascade(this.projectId())
            .pipe(
                finalize(() => (this.deleting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                this.close();
                if (this.isResponseSucceed(res, true, 'Xóa dự án thành công')) {
                    this.deleted.emit();
                }
            });
    }
}
