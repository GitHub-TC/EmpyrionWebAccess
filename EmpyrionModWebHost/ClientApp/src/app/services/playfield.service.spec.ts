import { TestBed } from '@angular/core/testing';

import { PlayfieldService } from './playfield.service';

describe('PlayfieldService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: PlayfieldService = TestBed.get(PlayfieldService);
    expect(service).toBeTruthy();
  });
});
