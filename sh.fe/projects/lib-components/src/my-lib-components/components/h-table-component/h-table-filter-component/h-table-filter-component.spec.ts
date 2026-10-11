import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HTableFilterComponent } from './h-table-filter-component';

describe('HTableFilterComponent', () => {
  let component: HTableFilterComponent;
  let fixture: ComponentFixture<HTableFilterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HTableFilterComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HTableFilterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
