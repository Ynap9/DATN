import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-error',
    imports: [ButtonModule, RouterModule],
    template: `
        <div class="bg-surface-50 flex items-center justify-center min-h-screen overflow-hidden">
            <div class="flex flex-col items-center justify-center">
                <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, rgba(233, 30, 99, 0.4) 10%, rgba(33, 150, 243, 0) 30%)">
                    <div class="w-full bg-surface-0 py-20 px-8 sm:px-20 flex flex-col items-center" style="border-radius: 53px">
                        <div class="gap-4 flex flex-col items-center">
                            <div class="flex justify-center items-center border-2 border-pink-500 rounded-full" style="height: 3.2rem; width: 3.2rem">
                                <i class="pi pi-fw pi-exclamation-circle text-2xl! text-pink-500"></i>
                            </div>
                            <h1 class="text-surface-900 font-bold text-4xl lg:text-5xl mb-2">Đã xảy ra lỗi</h1>
                            <span class="text-muted-color mb-8">Không thể tải nội dung bạn yêu cầu. Vui lòng thử lại sau.</span>
                            <p-button label="Về trang chủ" routerLink="/admin" severity="danger" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
})
export class ErrorComponent {}
