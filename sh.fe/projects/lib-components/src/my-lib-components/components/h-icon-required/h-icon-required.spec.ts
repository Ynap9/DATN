import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HIconRequiredComponent } from './h-icon-required';

describe('HIconRequiredComponent', () => {
  let component: HIconRequiredComponent;
  let fixture: ComponentFixture<HIconRequiredComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HIconRequiredComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HIconRequiredComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
