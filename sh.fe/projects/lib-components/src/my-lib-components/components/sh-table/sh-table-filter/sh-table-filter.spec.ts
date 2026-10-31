import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShTableFilterComponent } from './sh-table-filter';

describe('ShTableFilterComponent', () => {
  let component: ShTableFilterComponent;
  let fixture: ComponentFixture<ShTableFilterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShTableFilterComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShTableFilterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
