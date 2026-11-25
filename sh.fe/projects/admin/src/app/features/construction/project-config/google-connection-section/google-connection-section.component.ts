import { Component, input, output } from '@angular/core';
import { SharedImports } from '@/shared/import.shared';

export interface IGoogleConnectResult {
    isSuccess: boolean;
    message: string;
}

// Khối Kết nối: trạng thái kết nối Google, nút Kết nối và kết quả lần quay về từ Google (50.4, 50.5)
@Component({
    selector: 'app-google-connection-section',
    imports: [SharedImports],
    templateUrl: './google-connection-section.component.html',
})
export class GoogleConnectionSectionComponent {
    isConnected = input.required<boolean>();
    canConnect = input.required<boolean>();
    connecting = input.required<boolean>();
    result = input<IGoogleConnectResult | null>(null);
    connect = output<void>();
}
