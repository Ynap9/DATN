import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-access',
    imports: [ButtonModule, RouterModule],
    template: `
        <div class="bg-surface-50 flex items-center justify-center min-h-screen overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, rgba(247, 149, 48, 0.4) 10%, rgba(247, 149, 48, 0) 30%)">
                    <div class="w-full bg-surface-0 py-20 px-8 sm:px-20 flex flex-col items-center" style="border-radius: 53px">
                        <div class="gap-4 flex flex-col items-center">
                            <div class="flex justify-center items-center border-2 border-orange-500 rounded-full" style="width: 3.2rem; height: 3.2rem">
                                <i class="text-orange-500 pi pi-fw pi-lock text-2xl!"></i>
                            </div>
                            <h1 class="text-surface-900 font-bold text-4xl lg:text-5xl mb-2">Không có quyền truy cập</h1>
                            <span class="text-muted-color mb-8">Bạn không có quyền vào chức năng này. Vui lòng liên hệ quản trị viên.</span>
                            <p-button label="Về trang chủ" routerLink="/admin" severity="warn" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
})
export class AccessComponent {}
