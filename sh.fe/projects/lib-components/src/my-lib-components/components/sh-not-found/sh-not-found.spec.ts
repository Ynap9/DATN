import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShNotFoundComponent } from './sh-not-found';

describe('ShNotFoundComponent', () => {
  let component: ShNotFoundComponent;
  let fixture: ComponentFixture<ShNotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShNotFoundComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShNotFoundComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
