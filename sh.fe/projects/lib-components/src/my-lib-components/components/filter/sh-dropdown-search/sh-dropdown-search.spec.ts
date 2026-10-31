import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShDropdownSearchComponent } from './sh-dropdown-search';

describe('ShDropdownSearchComponent', () => {
  let component: ShDropdownSearchComponent;
  let fixture: ComponentFixture<ShDropdownSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShDropdownSearchComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShDropdownSearchComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
