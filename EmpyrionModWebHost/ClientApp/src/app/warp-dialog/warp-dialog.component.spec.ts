import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WarpDialogComponent } from './warp-dialog.component';

describe('WarpDialogComponent', () => {
  let component: WarpDialogComponent;
  let fixture: ComponentFixture<WarpDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WarpDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WarpDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
