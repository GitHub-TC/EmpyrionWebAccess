import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModConfigurationComponent } from './mod-configuration.component';

describe('ModConfigurationComponent', () => {
  let component: ModConfigurationComponent;
  let fixture: ComponentFixture<ModConfigurationComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ModConfigurationComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModConfigurationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
