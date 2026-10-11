import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HSelectComponent } from './h-select';

describe('HSelectComponent', () => {
  let component: HSelectComponent;
  let fixture: ComponentFixture<HSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HSelectComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HSelectComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
