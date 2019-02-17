import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { GalaxyMapComponent } from './galaxy-map.component';

describe('GalaxyMapComponent', () => {
  let component: GalaxyMapComponent;
  let fixture: ComponentFixture<GalaxyMapComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ GalaxyMapComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GalaxyMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
