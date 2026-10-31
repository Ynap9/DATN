import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShCalendarComponent } from './sh-calendar';

describe('ShCalendarComponent', () => {
  let component: ShCalendarComponent;
  let fixture: ComponentFixture<ShCalendarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShCalendarComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShCalendarComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
