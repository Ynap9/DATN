import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShButtonComponent } from './sh-button';

describe('ShButtonComponent', () => {
  let component: ShButtonComponent;
  let fixture: ComponentFixture<ShButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShButtonComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShButtonComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
