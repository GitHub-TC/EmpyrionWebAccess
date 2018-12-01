import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RestorePlayfieldComponent } from './restore-playfield.component';

describe('RestorePlayfieldComponent', () => {
  let component: RestorePlayfieldComponent;
  let fixture: ComponentFixture<RestorePlayfieldComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RestorePlayfieldComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RestorePlayfieldComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
