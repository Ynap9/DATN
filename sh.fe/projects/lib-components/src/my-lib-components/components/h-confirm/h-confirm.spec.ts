import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HConfirmComponent } from './h-confirm';

describe('HConfirmComponent', () => {
  let component: HConfirmComponent;
  let fixture: ComponentFixture<HConfirmComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HConfirmComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HConfirmComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
