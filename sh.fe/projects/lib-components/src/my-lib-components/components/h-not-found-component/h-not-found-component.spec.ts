import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HNotFoundComponent } from './h-not-found-component';

describe('HNotFoundComponent', () => {
  let component: HNotFoundComponent;
  let fixture: ComponentFixture<HNotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HNotFoundComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HNotFoundComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
