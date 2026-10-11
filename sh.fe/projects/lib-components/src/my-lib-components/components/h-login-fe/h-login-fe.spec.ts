import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HLoginFeComponent } from './h-login-fe';

describe('HLoginFeComponent', () => {
  let component: HLoginFeComponent;
  let fixture: ComponentFixture<HLoginFeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HLoginFeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HLoginFeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
