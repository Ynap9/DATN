import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShInputNumberComponent } from './sh-input-number';

describe('ShInputNumberComponent', () => {
  let component: ShInputNumberComponent;
  let fixture: ComponentFixture<ShInputNumberComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShInputNumberComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShInputNumberComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
