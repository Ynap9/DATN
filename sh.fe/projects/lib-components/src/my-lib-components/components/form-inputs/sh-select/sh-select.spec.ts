import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShSelectComponent } from './sh-select';

describe('ShSelectComponent', () => {
  let component: ShSelectComponent;
  let fixture: ComponentFixture<ShSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShSelectComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShSelectComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
