import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { StructuresListComponent } from './structures-list.component';

describe('StructuresListComponent', () => {
  let component: StructuresListComponent;
  let fixture: ComponentFixture<StructuresListComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ StructuresListComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(StructuresListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
