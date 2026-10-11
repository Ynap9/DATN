import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HCalendarComponent } from './h-calendar';

describe('HCalendarComponent', () => {
  let component: HCalendarComponent;
  let fixture: ComponentFixture<HCalendarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HCalendarComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HCalendarComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
