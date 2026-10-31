import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShLoadingComponent } from './sh-loading';

describe('ShLoadingComponent', () => {
  let component: ShLoadingComponent;
  let fixture: ComponentFixture<ShLoadingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShLoadingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShLoadingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
