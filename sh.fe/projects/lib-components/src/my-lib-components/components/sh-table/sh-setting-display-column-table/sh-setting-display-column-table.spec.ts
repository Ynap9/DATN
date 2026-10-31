import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShSettingDisplayColumnTableComponent } from './sh-setting-display-column-table';

describe('ShSettingDisplayColumnTableComponent', () => {
  let component: ShSettingDisplayColumnTableComponent;
  let fixture: ComponentFixture<ShSettingDisplayColumnTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShSettingDisplayColumnTableComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShSettingDisplayColumnTableComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
