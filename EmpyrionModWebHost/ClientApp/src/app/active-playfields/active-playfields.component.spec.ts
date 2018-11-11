import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ActivePlayfieldsComponent } from './active-playfields.component';

describe('ActivePlayfieldsComponent', () => {
  let component: ActivePlayfieldsComponent;
  let fixture: ComponentFixture<ActivePlayfieldsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ActivePlayfieldsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ActivePlayfieldsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
