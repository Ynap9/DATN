import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShBreadcrumbComponent } from './sh-breadcrumb';

describe('ShBreadcrumbComponent', () => {
  let component: ShBreadcrumbComponent;
  let fixture: ComponentFixture<ShBreadcrumbComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShBreadcrumbComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShBreadcrumbComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
