import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppFloatingConfigurator } from './app-floating-configurator';

describe('AppFloatingConfigurator', () => {
  let component: AppFloatingConfigurator;
  let fixture: ComponentFixture<AppFloatingConfigurator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AppFloatingConfigurator]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppFloatingConfigurator);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
