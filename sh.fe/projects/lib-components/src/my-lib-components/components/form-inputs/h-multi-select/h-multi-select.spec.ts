import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HMultiSelect } from './h-multi-select';

describe('HMultiSelect', () => {
  let component: HMultiSelect;
  let fixture: ComponentFixture<HMultiSelect>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HMultiSelect]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HMultiSelect);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
