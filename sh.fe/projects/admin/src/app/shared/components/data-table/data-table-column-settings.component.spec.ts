import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { DataTableColumnSettings, IColumnSettingsDialogData } from './data-table-column-settings.component';

describe('DataTableColumnSettings', () => {
    const data: IColumnSettingsDialogData = {
        items: [
            { key: 'c', header: 'C', visible: true },
            { key: 'a', header: 'A', visible: false },
        ],
        defaults: [
            { key: 'a', header: 'A', visible: true },
            { key: 'c', header: 'C', visible: true },
        ],
        lockedHeaders: ['STT', 'Thao tác'],
    };
    let closed: unknown[];

    function create(): DataTableColumnSettings {
        closed = [];
        TestBed.configureTestingModule({
            providers: [
                provideNoopAnimations(),
                providePrimeNG(),
                { provide: DynamicDialogConfig, useValue: { data } },
                { provide: DynamicDialogRef, useValue: { close: (value: unknown) => closed.push(value) } },
            ],
        });
        const fixture = TestBed.createComponent(DataTableColumnSettings);
        fixture.detectChanges();
        return fixture.componentInstance;
    }

    it('lưu trả về key + visible theo thứ tự đang xếp, không sửa dữ liệu truyền vào', () => {
        const settings = create();

        settings.items[1].visible = true;
        settings.save();

        expect(closed).toEqual([
            [
                { key: 'c', visible: true },
                { key: 'a', visible: true },
            ],
        ]);
        expect(data.items[1].visible).toBeFalse();
    });

    it('đặt lại về mặc định, bỏ chọn hết thì đếm 0', () => {
        const settings = create();

        settings.reset();
        expect(settings.items.map((item) => item.key)).toEqual(['a', 'c']);

        settings.toggleAll(false);
        expect(settings.checkedCount).toBe(0);
    });
});
