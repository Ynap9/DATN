import { Component, input } from '@angular/core';
import { formatNumber, IMPORT_VERIFY_NOTE, IVerifyView } from '../data-transfer.constants';

// Bước 4 wizard import: tóm tắt lựa chọn + số chạy thử, khung đang import / lỗi (48.11, 48.13); chỉ hiển thị
@Component({
    selector: 'app-import-verify-step',
    templateUrl: './import-verify-step.component.html',
})
export class ImportVerifyStepComponent {
    chips = input.required<string[]>();
    view = input.required<IVerifyView>();
    runningMessage = input<string | null>(null);
    errorMessage = input<string | null>(null);

    readonly formatNumber = formatNumber;
    readonly note = IMPORT_VERIFY_NOTE;
}
