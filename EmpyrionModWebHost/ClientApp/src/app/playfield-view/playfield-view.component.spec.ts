import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayfieldViewComponent } from './playfield-view.component';

describe('PlayfieldViewComponent', () => {
  let component: PlayfieldViewComponent;
  let fixture: ComponentFixture<PlayfieldViewComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlayfieldViewComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayfieldViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
