import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShMultiSelectComponent } from './sh-multi-select';

describe('ShMultiSelectComponent', () => {
  let component: ShMultiSelectComponent;
  let fixture: ComponentFixture<ShMultiSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShMultiSelectComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShMultiSelectComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
