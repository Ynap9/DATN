import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HInputSearch } from './h-input-search';

describe('HInputSearch', () => {
  let component: HInputSearch;
  let fixture: ComponentFixture<HInputSearch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HInputSearch]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HInputSearch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
