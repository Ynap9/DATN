import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShInputTextComponent } from './sh-input-text';

describe('ShInputTextComponent', () => {
  let component: ShInputTextComponent;
  let fixture: ComponentFixture<ShInputTextComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShInputTextComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShInputTextComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
