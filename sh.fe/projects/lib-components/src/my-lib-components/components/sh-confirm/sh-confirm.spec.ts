import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShConfirmComponent } from './sh-confirm';

describe('ShConfirmComponent', () => {
  let component: ShConfirmComponent;
  let fixture: ComponentFixture<ShConfirmComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShConfirmComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShConfirmComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
