import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RestoreFactoryItemsComponent } from './restore-FactoryItems.component';

describe('RestoreFactoryItemsComponent', () => {
  let component: RestoreFactoryItemsComponent;
  let fixture: ComponentFixture<RestoreFactoryItemsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RestoreFactoryItemsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RestoreFactoryItemsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
