import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RestoreStructureComponent } from './restore-structure.component';

describe('RestoreStructureComponent', () => {
  let component: RestoreStructureComponent;
  let fixture: ComponentFixture<RestoreStructureComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RestoreStructureComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RestoreStructureComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
