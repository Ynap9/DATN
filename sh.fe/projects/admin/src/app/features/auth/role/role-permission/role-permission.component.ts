import { Component, computed, DestroyRef, effect, inject, input, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CheckboxModule } from 'primeng/checkbox';
import { finalize } from 'rxjs';
import { IRolePermissionTree, IUpdateRolePermissionNode, IViewRole } from '@/models/role.model';
import { RoleService } from '@/services/role.service';
import { BaseComponent } from '@/shared/base/base.component';
import { AuthConstants } from '@/shared/constants/auth.constants';
import { SharedImports } from '@/shared/import.shared';

const MENU_LABEL_PREFIX = /^Menu\s+/i;

@Component({
    selector: 'app-role-permission',
    imports: [SharedImports, CheckboxModule],
    templateUrl: './role-permission.component.html',
    styleUrl: './role-permission.component.scss',
})
export class RolePermissionComponent extends BaseComponent {
    private readonly _roleService = inject(RoleService);
    private readonly _destroyRef = inject(DestroyRef);

    role = input.required<IViewRole>();

    private readonly _roleId = computed(() => this.role().id);
    private _checkedKeys = new Set<string>();
    private _savedKeys = '';

    sections: IRolePermissionTree[] = [];
    collapsedKeys = new Set<string>();
    keyword = '';
    submitting = false;

    get isSuperAdmin(): boolean {
        return this.role().name === AuthConstants.SUPER_ADMIN_ROLE;
    }

    get isDirty(): boolean {
        return this.serializeCheckedKeys() !== this._savedKeys;
    }

    get visibleSections(): IRolePermissionTree[] {
        return this.sections.filter((section) => this.isMatched(section));
    }

    get checkedCount(): number {
        return this._checkedKeys.size;
    }

    get totalCount(): number {
        return this.sections.reduce((total, section) => total + this.getLeaves(section).length, 0);
    }

    constructor() {
        super();
        effect(() => {
            const roleId = this._roleId();
            untracked(() => this.load(roleId));
        });
    }

    getLabel(node: IRolePermissionTree): string {
        const label = node.description.replace(MENU_LABEL_PREFIX, '');
        return label.charAt(0).toUpperCase() + label.slice(1);
    }

    getVisibleGroups(section: IRolePermissionTree): IRolePermissionTree[] {
        return this.isLabelMatched(section) ? section.children : section.children.filter((group) => this.isMatched(group));
    }

    getVisibleLeaves(group: IRolePermissionTree): IRolePermissionTree[] {
        if (!group.children.length) return [];

        const leaves = this.getLeaves(group);
        return this.isLabelMatched(group) ? leaves : leaves.filter((leaf) => this.isLabelMatched(leaf));
    }

    getCheckedCount(node: IRolePermissionTree): number {
        return this.getLeaves(node).filter((leaf) => this._checkedKeys.has(leaf.key)).length;
    }

    getLeafCount(node: IRolePermissionTree): number {
        return this.getLeaves(node).length;
    }

    isChecked(node: IRolePermissionTree): boolean {
        return this.getCheckedCount(node) === this.getLeafCount(node);
    }

    isPartial(node: IRolePermissionTree): boolean {
        const count = this.getCheckedCount(node);
        return count > 0 && count < this.getLeafCount(node);
    }

    toggle(node: IRolePermissionTree, checked: boolean) {
        this.getLeaves(node).forEach((leaf) => (checked ? this._checkedKeys.add(leaf.key) : this._checkedKeys.delete(leaf.key)));
    }

    toggleCollapse(key: string) {
        if (!this.collapsedKeys.delete(key)) {
            this.collapsedKeys.add(key);
        }
    }

    reset() {
        this._checkedKeys = new Set(this._savedKeys ? this._savedKeys.split(',') : []);
    }

    onSubmit() {
        this.submitting = true;
        this._roleService
            .updatePermission({ roleId: this.role().id, permissions: this.sections.map((section) => this.toPermissionNode(section)) })
            .pipe(
                finalize(() => (this.submitting = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res, true, 'Cập nhật quyền thành công')) {
                    this._savedKeys = this.serializeCheckedKeys();
                }
            });
    }

    private load(roleId: number) {
        this.loading = true;
        this.sections = [];
        this._roleService
            .getPermissionTree(roleId)
            .pipe(
                finalize(() => (this.loading = false)),
                takeUntilDestroyed(this._destroyRef),
            )
            .subscribe((res) => {
                if (this.isResponseSucceed(res)) {
                    this.sections = res.data;
                    this._checkedKeys = new Set(res.data.flatMap((section) => this.getLeaves(section)).filter((leaf) => leaf.isChecked).map((leaf) => leaf.key));
                    this._savedKeys = this.serializeCheckedKeys();
                }
            });
    }

    private getLeaves(node: IRolePermissionTree): IRolePermissionTree[] {
        return node.children.length ? node.children.flatMap((child) => this.getLeaves(child)) : [node];
    }

    private isLabelMatched(node: IRolePermissionTree): boolean {
        return node.description.toLowerCase().includes(this.keyword.trim().toLowerCase());
    }

    private isMatched(node: IRolePermissionTree): boolean {
        return this.isLabelMatched(node) || node.children.some((child) => this.isMatched(child));
    }

    private serializeCheckedKeys(): string {
        return [...this._checkedKeys].sort().join(',');
    }

    private toPermissionNode(node: IRolePermissionTree): IUpdateRolePermissionNode {
        return {
            key: node.key,
            isChecked: this.isChecked(node),
            children: node.children.map((child) => this.toPermissionNode(child)),
        };
    }
}
