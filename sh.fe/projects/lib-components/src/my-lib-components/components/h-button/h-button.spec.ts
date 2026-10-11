import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HButtonComponent } from './h-button';

describe('HButtonComponent', () => {
  let component: HButtonComponent;
  let fixture: ComponentFixture<HButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HButtonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HButtonComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
