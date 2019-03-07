import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayfieldPlanetview3dComponent } from './playfield-planetview3d.component';

describe('PlayfieldPlanetview3dComponent', () => {
  let component: PlayfieldPlanetview3dComponent;
  let fixture: ComponentFixture<PlayfieldPlanetview3dComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlayfieldPlanetview3dComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlayfieldPlanetview3dComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
