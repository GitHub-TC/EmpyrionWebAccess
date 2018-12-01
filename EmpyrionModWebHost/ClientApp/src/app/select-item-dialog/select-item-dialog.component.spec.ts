import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectItemDialogComponent } from './select-item-dialog.component';

describe('SelectItemDialogComponent', () => {
  let component: SelectItemDialogComponent;
  let fixture: ComponentFixture<SelectItemDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SelectItemDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectItemDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
