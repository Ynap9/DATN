import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HPasswordComponent } from './h-password-component';

describe('HPasswordComponent', () => {
  let component: HPasswordComponent;
  let fixture: ComponentFixture<HPasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HPasswordComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HPasswordComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
