import { Component, DestroyRef, inject, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ShSelectComponent } from '@sh/ui';
import { IViewProjectPair } from '@/models/project.model';
import { ProjectService } from '@/services/project.service';

/**
 * Ô chọn Dự án + Loại tài liệu dùng chung cho các màn vận hành, nhớ cặp trên URL (?projectId=&documentTypeId=).
 * Chỉ giữ cặp có viewerRole trong `viewerRoles`; thiếu / sai cặp trên URL → ghi cặp đầu tiên (dự án sắp theo tên).
 * Phát `pairChange` khi cặp đổi (hoặc khi gọi reload), `pairsLoaded` sau mỗi lần tải danh sách cặp.
 */
@Component({
    selector: 'app-project-pair-picker',
    imports: [FormsModule, ShSelectComponent],
    template: `
        <div class="flex flex-wrap items-end gap-4">
            <sh-select class="w-full sm:w-80" label="Dự án" [options]="projects" optionLabel="projectName" optionValue="projectId" [filter]="true"
                placeholder="Chọn dự án" [ngModel]="pair?.projectId" (ngModelChange)="onProjectChange($event)" />
            <sh-select class="w-full sm:w-64" label="Loại tài liệu" [options]="documentTypes" optionLabel="documentTypeName" optionValue="documentTypeId"
                placeholder="Chọn loại tài liệu" [ngModel]="pair?.documentTypeId" (ngModelChange)="onDocumentTypeChange($event)" />
        </div>
    `,
})
export class ProjectPairPicker {
    private readonly _projectService = inject(ProjectService);
    private readonly _router = inject(Router);
    private readonly _activatedRoute = inject(ActivatedRoute);
    private readonly _destroyRef = inject(DestroyRef);

    viewerRoles = input.required<string[]>();
    pairChange = output<IViewProjectPair | null>();
    pairsLoaded = output<IViewProjectPair[]>();

    pairs: IViewProjectPair[] = [];
    projects: { projectId: number; projectName: string }[] = [];
    documentTypes: IViewProjectPair[] = [];
    pair: IViewProjectPair | null = null;

    ngOnInit(): void {
        this.loadPairs(() => {
            this._activatedRoute.queryParamMap.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => this.applySelection(false));
        });
    }

    // Tải lại danh sách cặp (vd dự án vừa đóng / mất quyền) và luôn phát lại cặp đang chọn với dữ liệu mới
    reload() {
        this.loadPairs(() => this.applySelection(true));
    }

    // sh-select phát lại giá trị lúc khởi tạo nên so với cặp đang chọn; đổi dự án thì lấy loại tài liệu đầu tiên
    onProjectChange(projectId: number) {
        if (projectId === this.pair?.projectId) return;
        const first = this.pairs.find((item) => item.projectId === projectId);
        if (first) {
            this.selectPair(first.projectId, first.documentTypeId);
        }
    }

    onDocumentTypeChange(documentTypeId: number) {
        if (!this.pair || documentTypeId === this.pair.documentTypeId) return;
        this.selectPair(this.pair.projectId, documentTypeId);
    }

    // Lấy hết cặp một lần, giữ cặp đúng vai trò người xem; dự án khác nhau sắp theo tên
    private loadPairs(onLoaded: () => void) {
        this._projectService
            .findPaging({ pageNumber: 1, pageSize: -1 })
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe((res) => {
                if (!res || res.status !== 1) return;
                const viewerRoles = this.viewerRoles();
                this.pairs = res.data.items.filter((item) => viewerRoles.includes(item.viewerRole));
                const projectNames = new Map(this.pairs.map((item) => [item.projectId, item.projectName]));
                this.projects = [...projectNames]
                    .map(([projectId, projectName]) => ({ projectId, projectName }))
                    .sort((a, b) => a.projectName.localeCompare(b.projectName, 'vi'));
                this.pairsLoaded.emit(this.pairs);
                onLoaded();
            });
    }

    // Đọc cặp từ URL; thiếu / không còn trong danh sách thì ghi cặp đầu tiên lên URL (queryParamMap gọi lại hàm này)
    private applySelection(isForced: boolean) {
        const params = this._activatedRoute.snapshot.queryParamMap;
        const projectId = Number(params.get('projectId'));
        const documentTypeId = Number(params.get('documentTypeId'));
        const found = this.pairs.find((item) => item.projectId === projectId && item.documentTypeId === documentTypeId);
        if (!found) {
            if (this.pair) {
                this.pair = null;
                this.pairChange.emit(null);
            }
            if (this.pairs.length) {
                const first = this.pairs.find((item) => item.projectId === this.projects[0].projectId)!;
                this.selectPair(first.projectId, first.documentTypeId);
            }
            return;
        }
        const isPairChanged = found.projectId !== this.pair?.projectId || found.documentTypeId !== this.pair?.documentTypeId;
        this.pair = found;
        this.documentTypes = this.pairs.filter((item) => item.projectId === found.projectId);
        if (isPairChanged || isForced) {
            this.pairChange.emit(found);
        }
    }

    // Giữ các tham số lọc khác của màn trên URL
    private selectPair(projectId: number, documentTypeId: number) {
        this._router.navigate([], { relativeTo: this._activatedRoute, queryParams: { projectId, documentTypeId }, queryParamsHandling: 'merge', replaceUrl: true });
    }
}
