import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { IColumn, ITableEvent } from '@/shared/models/data-table.models';
import { TblAction } from '../tbl-action/tbl-action.component';
import { DataTable } from './data-table.component';

describe('DataTable', () => {
    let fixture: ComponentFixture<DataTable>;

    const columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX },
        { header: 'Tên', field: 'info.name' },
        { header: 'Tiền', field: 'amount', cellViewType: CellViewTypes.CURRENCY },
        { header: 'Thao tác', cellViewType: CellViewTypes.ACTIONS, actions: [{ type: 'delete', label: 'Xoá' }] },
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
        TestBed.configureTestingModule({ providers: [provideNoopAnimations(), providePrimeNG()] });
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
        action.menuItems()[0].command({});

        expect(events).toEqual([{ type: 'delete', row }]);
    });
});
