import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppConfigurator } from './app-configurator';

describe('AppConfigurator', () => {
  let component: AppConfigurator;
  let fixture: ComponentFixture<AppConfigurator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppConfigurator]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppConfigurator);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
