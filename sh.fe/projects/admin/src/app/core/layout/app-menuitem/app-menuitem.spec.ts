import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppMenuitem } from './app-menuitem';

describe('AppMenuitem', () => {
  let component: AppMenuitem;
  let fixture: ComponentFixture<AppMenuitem>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppMenuitem]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppMenuitem);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
