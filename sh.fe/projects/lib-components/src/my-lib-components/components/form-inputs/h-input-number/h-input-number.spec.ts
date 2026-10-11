import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HInputNumberComponent } from './h-input-number';

describe('HInputNumberComponent', () => {
  let component: HInputNumberComponent;
  let fixture: ComponentFixture<HInputNumberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HInputNumberComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HInputNumberComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
