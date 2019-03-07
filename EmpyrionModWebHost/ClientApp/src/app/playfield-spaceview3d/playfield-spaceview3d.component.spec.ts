import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayfieldSpaceview3dComponent } from './playfield-spaceview3d.component';

describe('PlayfieldSpaceview3dComponent', () => {
  let component: PlayfieldSpaceview3dComponent;
  let fixture: ComponentFixture<PlayfieldSpaceview3dComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlayfieldSpaceview3dComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayfieldSpaceview3dComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
