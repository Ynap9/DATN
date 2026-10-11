import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HTableComponent } from './h-table-component';

describe('HTableComponent', () => {
  let component: HTableComponent;
  let fixture: ComponentFixture<HTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HTableComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HTableComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
