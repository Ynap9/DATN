import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShPasswordComponent } from './sh-password';

describe('ShPasswordComponent', () => {
  let component: ShPasswordComponent;
  let fixture: ComponentFixture<ShPasswordComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShPasswordComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShPasswordComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
