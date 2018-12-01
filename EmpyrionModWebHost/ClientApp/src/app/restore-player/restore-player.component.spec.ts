import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RestorePlayerComponent } from './restore-player.component';

describe('RestorePlayerComponent', () => {
  let component: RestorePlayerComponent;
  let fixture: ComponentFixture<RestorePlayerComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RestorePlayerComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RestorePlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
