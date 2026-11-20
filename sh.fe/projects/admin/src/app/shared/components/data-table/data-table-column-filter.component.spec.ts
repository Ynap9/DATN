import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { IColumn } from '@/shared/models/data-table.models';
import { DataTableColumnFilter, hasFilterValue } from './data-table-column-filter.component';

describe('DataTableColumnFilter', () => {
    let fixture: ComponentFixture<DataTableColumnFilter>;
    let emitted: unknown[];

    function render(column: IColumn, value?: unknown): DataTableColumnFilter {
        fixture.componentRef.setInput('column', column);
        fixture.componentRef.setInput('value', value);
        fixture.detectChanges();
        return fixture.componentInstance;
    }

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideNoopAnimations(), providePrimeNG()] });
        fixture = TestBed.createComponent(DataTableColumnFilter);
        emitted = [];
        fixture.componentInstance.apply.subscribe((value) => emitted.push(value));
    });

    it('hasFilterValue coi chuỗi trống và khoảng ngày rỗng là không lọc', () => {
        expect([
            hasFilterValue(''),
            hasFilterValue(null),
            hasFilterValue({}),
            hasFilterValue({ to: '2026-01-01' }),
            hasFilterValue(0),
        ]).toEqual([false, false, false, true, true]);
    });

    it('đang có giá trị thì icon dạng fill', () => {
        render({ header: 'Tên', filter: { type: 'text' } }, 'An');

        expect(fixture.nativeElement.querySelector('.pi-filter-fill')).not.toBeNull();
    });

    it('text: cắt khoảng trắng, chuỗi trống thì phát null', () => {
        const filter = render({ header: 'Tên', filter: { type: 'text' } });

        filter.text = '  admin ';
        filter.applyDraft();
        filter.text = '   ';
        filter.applyDraft();

        expect(emitted).toEqual(['admin', null]);
    });

    it('dateRange: Từ > Đến thì không phát', () => {
        const filter = render({ header: 'Ngày', filter: { type: 'dateRange' } });

        filter.from = '2026-02-01';
        filter.to = '2026-01-01';
        filter.applyDraft();
        filter.to = '2026-02-01';
        filter.applyDraft();

        expect(filter.invalidRange).toBeFalse();
        expect(emitted).toEqual([{ from: '2026-02-01', to: '2026-02-01' }]);
    });
});
