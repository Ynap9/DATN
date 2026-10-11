import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HbreadcrumbComponent } from './hbreadcrumb-component';

describe('HbreadcrumbComponent', () => {
  let component: HbreadcrumbComponent;
  let fixture: ComponentFixture<HbreadcrumbComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HbreadcrumbComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HbreadcrumbComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
