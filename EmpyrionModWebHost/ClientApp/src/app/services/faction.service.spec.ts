import { TestBed } from '@angular/core/testing';

import { FactionService } from './faction.service';

describe('FactionService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: FactionService = TestBed.get(FactionService);
    expect(service).toBeTruthy();
  });
});
