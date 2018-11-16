import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayerBackpackComponent } from './player-backpack.component';

describe('PlayerBackpackComponent', () => {
  let component: PlayerBackpackComponent;
  let fixture: ComponentFixture<PlayerBackpackComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlayerBackpackComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayerBackpackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
