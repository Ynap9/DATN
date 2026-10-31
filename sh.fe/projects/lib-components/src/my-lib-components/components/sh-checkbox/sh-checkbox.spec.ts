import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShCheckboxComponent } from './sh-checkbox';

describe('ShCheckboxComponent', () => {
  let component: ShCheckboxComponent;
  let fixture: ComponentFixture<ShCheckboxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShCheckboxComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShCheckboxComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
