import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ServerModManagerComponent } from './server-mod-manager.component';

describe('ServerModManagerComponent', () => {
  let component: ServerModManagerComponent;
  let fixture: ComponentFixture<ServerModManagerComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ServerModManagerComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ServerModManagerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
