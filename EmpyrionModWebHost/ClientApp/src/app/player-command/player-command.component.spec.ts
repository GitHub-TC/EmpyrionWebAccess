import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayerNoteComponent } from './player-note.component';

describe('PlayerNoteComponent', () => {
  let component: PlayerNoteComponent;
  let fixture: ComponentFixture<PlayerNoteComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlayerNoteComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayerNoteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
