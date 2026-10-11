import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HSettingDisplayColumnTable } from './h-setting-display-column-table';

describe('HSettingDisplayColumnTable', () => {
  let component: HSettingDisplayColumnTable;
  let fixture: ComponentFixture<HSettingDisplayColumnTable>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HSettingDisplayColumnTable]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HSettingDisplayColumnTable);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
