import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HDropdownSearch } from './h-dropdown-search';

describe('HDropdownSearch', () => {
  let component: HDropdownSearch;
  let fixture: ComponentFixture<HDropdownSearch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HDropdownSearch]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HDropdownSearch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
