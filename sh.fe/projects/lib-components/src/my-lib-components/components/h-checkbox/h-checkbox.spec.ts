import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HCheckboxComponent } from './h-checkbox';

describe('HCheckboxComponent', () => {
  let component: HCheckboxComponent;
  let fixture: ComponentFixture<HCheckboxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HCheckboxComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HCheckboxComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
