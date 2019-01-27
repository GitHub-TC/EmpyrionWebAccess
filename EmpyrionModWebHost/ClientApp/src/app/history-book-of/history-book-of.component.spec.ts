import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HistoryBookOfComponent } from './history-book-of.component';

describe('HistoryBookOfComponent', () => {
  let component: HistoryBookOfComponent;
  let fixture: ComponentFixture<HistoryBookOfComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HistoryBookOfComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HistoryBookOfComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
