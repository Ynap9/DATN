import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShInputSearchComponent } from './sh-input-search';

describe('ShInputSearchComponent', () => {
  let component: ShInputSearchComponent;
  let fixture: ComponentFixture<ShInputSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShInputSearchComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShInputSearchComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
