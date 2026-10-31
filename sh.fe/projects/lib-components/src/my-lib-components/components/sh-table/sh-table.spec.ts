import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShTableComponent } from './sh-table';

describe('ShTableComponent', () => {
  let component: ShTableComponent;
  let fixture: ComponentFixture<ShTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShTableComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShTableComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
