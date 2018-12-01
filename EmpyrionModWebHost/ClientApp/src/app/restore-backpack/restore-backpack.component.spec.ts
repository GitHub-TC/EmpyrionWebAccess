import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RestoreBackpackComponent } from './restore-backpack.component';

describe('RestoreBackpackComponent', () => {
  let component: RestoreBackpackComponent;
  let fixture: ComponentFixture<RestoreBackpackComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RestoreBackpackComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RestoreBackpackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
