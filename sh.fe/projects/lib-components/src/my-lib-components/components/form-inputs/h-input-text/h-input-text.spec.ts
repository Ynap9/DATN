import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HInputTextComponent } from './h-input-text';

describe('HInputTextComponent', () => {
  let component: HInputTextComponent;
  let fixture: ComponentFixture<HInputTextComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HInputTextComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HInputTextComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
