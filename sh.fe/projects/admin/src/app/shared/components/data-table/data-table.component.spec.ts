import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { DialogService } from 'primeng/dynamicdialog';
import { CellViewTypes, TableEventTypes } from '@/shared/constants/data-table.constants';
import { IColumn, IColumnFilterValue, ITableEvent } from '@/shared/models/data-table.models';
import { TblAction } from '../tbl-action/tbl-action.component';
import { DataTable } from './data-table.component';

describe('DataTable', () => {
    let fixture: ComponentFixture<DataTable>;

    const columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX },
        { header: 'Tên', field: 'info.name' },
        { header: 'Tiền', field: 'amount', cellViewType: CellViewTypes.CURRENCY },
        { header: 'Thao tác', cellViewType: CellViewTypes.ACTIONS, actions: [{ type: 'delete', label: 'Xoá', icon: 'pi pi-trash' }] },
    ];

    function render(data: unknown[]): HTMLElement {
        fixture.componentRef.setInput('columns', columns);
        fixture.componentRef.setInput('data', data);
        fixture.componentRef.setInput('pageNumber', 2);
        fixture.componentRef.setInput('pageSize', 10);
        fixture.componentRef.setInput('total', 30);
        fixture.detectChanges();
        return fixture.nativeElement;
    }

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideNoopAnimations(), providePrimeNG(), DialogService] });
        fixture = TestBed.createComponent(DataTable);
    });

    it('hiện STT theo trang, trường lồng nhau và định dạng tiền', () => {
        const cells = render([{ id: 1, info: { name: 'An' }, amount: 1000 }]).querySelectorAll('tbody td');

        expect(cells[0].textContent.trim()).toBe('11');
        expect(cells[1].textContent.trim()).toBe('An');
        expect(cells[2].textContent.trim()).toBe(new Intl.NumberFormat('vi-VN').format(1000));
    });

    it('không có dữ liệu thì hiện thông báo', () => {
        expect(render([]).querySelector('tbody').textContent).toContain('Không có dữ liệu');
    });

    it('chọn thao tác thì phát type kèm dòng', () => {
        const row = { id: 1, info: { name: 'An' }, amount: 1 };
        const events: ITableEvent[] = [];
        fixture.componentInstance.action.subscribe((event) => events.push(event));
        render([row]);

        const action = fixture.debugElement.query((el) => el.componentInstance instanceof TblAction).componentInstance as TblAction;
        action.menuItems()[0].command!({});

        expect(events).toEqual([{ type: 'delete', row }]);
    });

    it('mặc định có chân phân trang, không có cột chọn và tay kéo', () => {
        const element = render([{ id: 1, info: { name: 'An' }, amount: 1 }]);

        expect(element.querySelector('.data-table-footer')).not.toBeNull();
        expect(element.querySelector('p-tableheadercheckbox')).toBeNull();
        expect(element.querySelector('[preorderablerowhandle]')).toBeNull();
    });

    it('paginator = false thì ẩn chân phân trang', () => {
        fixture.componentRef.setInput('paginator', false);

        expect(render([]).querySelector('.data-table-footer')).toBeNull();
    });

    it('selectable, reorderable thì thêm cột chọn và tay kéo, colspan dòng rỗng tính cả hai', () => {
        fixture.componentRef.setInput('selectable', true);
        fixture.componentRef.setInput('reorderable', true);
        const element = render([]);

        expect(element.querySelectorAll('thead th').length).toBe(columns.length + 2);
        expect(element.querySelector('p-tableheadercheckbox')).not.toBeNull();
        expect(element.querySelector('.data-table-empty')?.getAttribute('colspan')).toBe(String(columns.length + 2));
    });

    it('ô SWITCH bấm thì phát CELL_CLICK kèm field và dòng, khoá theo disabled', () => {
        const row = { id: 1, isShow: false };
        const events: ITableEvent[] = [];
        fixture.componentInstance.action.subscribe((event) => events.push(event));
        fixture.componentRef.setInput('columns', [{ header: 'Hiển thị', field: 'isShow', cellViewType: CellViewTypes.SWITCH, disabled: () => false }]);
        fixture.componentRef.setInput('data', [row]);
        fixture.detectChanges();

        (fixture.nativeElement.querySelector('p-toggleswitch input') as HTMLInputElement).click();

        expect(events).toEqual([{ type: TableEventTypes.CELL_CLICK, field: 'isShow', row }]);
    });

    it('ô PROGRESS làm tròn, chặn trong 0..100', () => {
        fixture.componentRef.setInput('columns', [{ header: 'Tiến độ', field: 'percent', cellViewType: CellViewTypes.PROGRESS }]);
        fixture.componentRef.setInput('data', [{ id: 1, percent: 62.4 }, { id: 2, percent: 130 }, { id: 3, percent: null }]);
        fixture.detectChanges();

        const values = Array.from(fixture.nativeElement.querySelectorAll('.data-table-progress-value')).map((el) => (el as HTMLElement).textContent!.trim());
        const fills = Array.from(fixture.nativeElement.querySelectorAll('.data-table-progress-fill')).map((el) => (el as HTMLElement).style.width);

        expect(values).toEqual(['62%', '100%', '0%']);
        expect(fills).toEqual(['62%', '100%', '0%']);
    });

    it('ô NUMBER định dạng vi-VN, thiếu field thì hiện —', () => {
        fixture.componentRef.setInput('columns', [
            { header: 'Số', field: 'total', cellViewType: CellViewTypes.NUMBER },
            { header: 'Chưa có', field: 'missing', cellViewType: CellViewTypes.NUMBER },
        ]);
        fixture.componentRef.setInput('data', [{ id: 1, total: 1234 }]);
        fixture.detectChanges();

        const cells = fixture.nativeElement.querySelectorAll('tbody td');
        expect(cells[0].textContent.trim()).toBe(new Intl.NumberFormat('vi-VN').format(1234));
        expect(cells[1].textContent.trim()).toBe('—');
    });

    it('ô BADGE theo tone, không có badge thì hiện —', () => {
        fixture.componentRef.setInput('columns', [
            { header: 'Trạng thái', cellViewType: CellViewTypes.BADGE, badge: () => ({ label: 'Hết hạn', tone: 'red' }) },
            { header: 'Khác', cellViewType: CellViewTypes.BADGE },
        ]);
        fixture.componentRef.setInput('data', [{ id: 1 }]);
        fixture.detectChanges();

        const cells = fixture.nativeElement.querySelectorAll('tbody td');
        expect(cells[0].querySelector('.status-badge--red')?.textContent.trim()).toBe('Hết hạn');
        expect(cells[1].textContent.trim()).toBe('—');
    });

    it('chỉ bật cuộn khi có cột frozen', () => {
        render([]);
        expect(fixture.nativeElement.querySelector('.p-datatable-scrollable')).toBeNull();

        fixture.componentRef.setInput('columns', [{ header: 'Tên', field: 'name', frozen: 'left' }, { header: 'Khác', field: 'other' }]);
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector('.p-datatable-scrollable')).not.toBeNull();
        expect(fixture.nativeElement.querySelector('thead th').classList).toContain('p-datatable-frozen-column');
    });

    it('độ rộng cột nhớ theo key, bỏ cache cũ khoá theo vị trí', fakeAsync(() => {
        localStorage.setItem('tbl_col_widths_spec', JSON.stringify({ 'info.name': 220, '0': 99 }));
        fixture.componentRef.setInput('tableKey', 'spec');
        render([{ id: 1, info: { name: 'An' }, amount: 1 }]);
        tick();

        const headers: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('thead th'));
        expect(headers.find((th) => th.dataset['colKey'] === 'info.name')!.style.width).toBe('220px');
        expect(headers.find((th) => th.dataset['colKey'] === 'STT')!.style.width).toBe('');
        localStorage.removeItem('tbl_col_widths_spec');
    }));

    describe('cấu hình cột', () => {
        const settingColumns: IColumn[] = [
            { header: 'STT', key: 'index', cellViewType: CellViewTypes.INDEX, locked: true },
            { header: 'A', field: 'a' },
            { header: 'B', field: 'b', hidden: true },
            { header: 'C', field: 'c' },
            { header: 'Thao tác', key: 'actions', cellViewType: CellViewTypes.ACTIONS, locked: true, actions: [] },
        ];

        function headers(): string[] {
            return Array.from(fixture.nativeElement.querySelectorAll('thead th')).map((th) => (th as HTMLElement).dataset['colKey']!);
        }

        function renderSettings(enabled: boolean): void {
            fixture.componentRef.setInput('columns', settingColumns);
            fixture.componentRef.setInput('data', []);
            fixture.componentRef.setInput('tableKey', 'spec');
            fixture.componentRef.setInput('columnSettings', enabled);
            fixture.detectChanges();
        }

        afterEach(() => localStorage.removeItem('tbl_cols_spec'));

        it('tắt thì hiện mọi cột như cũ, không có ⚙', () => {
            renderSettings(false);

            expect(headers()).toEqual(['index', 'a', 'b', 'c', 'actions']);
            expect(fixture.nativeElement.querySelector('.data-table-settings-icon')).toBeNull();
        });

        it('bật mà chưa lưu thì ẩn cột hidden, cột Thao tác thành ⚙', () => {
            renderSettings(true);

            expect(headers()).toEqual(['index', 'a', 'c', 'actions']);
            expect(fixture.nativeElement.querySelector('.data-table-settings-icon')).not.toBeNull();
        });

        it('áp cấu hình đã lưu, cột khoá giữ chỗ', () => {
            localStorage.setItem('tbl_cols_spec', JSON.stringify([{ key: 'c', visible: true }, { key: 'b', visible: true }, { key: 'a', visible: false }]));
            renderSettings(true);

            expect(headers()).toEqual(['index', 'c', 'b', 'actions']);
        });

        it('cache khác tập key hiện tại thì về mặc định', () => {
            localStorage.setItem('tbl_cols_spec', JSON.stringify([{ key: 'c', visible: true }, { key: 'x', visible: true }]));
            renderSettings(true);

            expect(headers()).toEqual(['index', 'a', 'c', 'actions']);
        });
    });

    it('không cột nào có group thì header một hàng như cũ', () => {
        const element = render([]);

        expect(element.querySelectorAll('thead tr').length).toBe(1);
        expect(element.querySelector('.data-table-group-start')).toBeNull();
    });

    it('cột liền nhau cùng group gộp colspan, cột đầu nhóm có vạch trái ở header và ô', () => {
        fixture.componentRef.setInput('columns', [
            { header: 'Tên', field: 'name' },
            { header: 'Đơn giá', field: 'a', group: 'Thực nhận' },
            { header: 'Thành tiền', field: 'b', group: 'Thực nhận' },
            { header: 'Đơn giá', field: 'c', group: 'Giá công ty' },
        ]);
        fixture.componentRef.setInput('data', [{ id: 1, name: 'An', a: 1, b: 2, c: 3 }]);
        fixture.detectChanges();

        const groupCells: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('thead tr.data-table-group-row th'));
        expect(groupCells.map((th) => [th.textContent!.trim(), th.getAttribute('colspan')])).toEqual([
            ['', '1'],
            ['Thực nhận', '2'],
            ['Giá công ty', '1'],
        ]);
        const bodyStarts = Array.from(fixture.nativeElement.querySelectorAll('tbody td')).map((td) => (td as HTMLElement).classList.contains('data-table-group-start'));
        expect(bodyStarts).toEqual([false, true, false, true]);
    });

    it('rowClass gắn class lên dòng theo dữ liệu', () => {
        fixture.componentRef.setInput('rowClass', (row: { id: number }) => (row.id === 2 ? 'row-marked' : ''));
        const rows = render([{ id: 1, info: { name: 'An' } }, { id: 2, info: { name: 'Bình' } }]).querySelectorAll('tbody tr');

        expect(rows[0].classList).not.toContain('row-marked');
        expect(rows[1].classList).toContain('row-marked');
    });

    it('lọc cột phát cả map, xoá lọc thì bỏ key', () => {
        const events: IColumnFilterValue[] = [];
        fixture.componentInstance.filterChange.subscribe((value) => events.push(value));
        fixture.componentRef.setInput('filterValues', { name: 'An' });
        const column: IColumn = { header: 'Ngày', field: 'date', filter: { type: 'dateRange' } };

        fixture.componentInstance.onFilterApply(column, { from: '2026-01-01' });
        fixture.componentInstance.onFilterApply({ header: 'Tên', field: 'name' }, null);

        expect(events).toEqual([{ name: 'An', date: { from: '2026-01-01' } }, {}]);
    });
});
