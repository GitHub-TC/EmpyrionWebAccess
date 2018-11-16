import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SysteminfoComponent } from './systeminfo.component';

describe('SysteminfoComponent', () => {
  let component: SysteminfoComponent;
  let fixture: ComponentFixture<SysteminfoComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SysteminfoComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SysteminfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
