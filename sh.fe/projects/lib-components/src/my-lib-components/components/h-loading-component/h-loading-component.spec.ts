import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HLoadingComponent } from './h-loading-component';

describe('HLoadingComponent', () => {
  let component: HLoadingComponent;
  let fixture: ComponentFixture<HLoadingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HLoadingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HLoadingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
