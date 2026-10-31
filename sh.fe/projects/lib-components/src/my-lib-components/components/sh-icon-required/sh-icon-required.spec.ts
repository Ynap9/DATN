import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShIconRequiredComponent } from './sh-icon-required';

describe('ShIconRequiredComponent', () => {
  let component: ShIconRequiredComponent;
  let fixture: ComponentFixture<ShIconRequiredComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShIconRequiredComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShIconRequiredComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
