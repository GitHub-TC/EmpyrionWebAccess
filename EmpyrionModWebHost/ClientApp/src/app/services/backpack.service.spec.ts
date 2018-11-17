import { TestBed } from '@angular/core/testing';

import { BackpackService } from './backpack.service';

describe('BackpackService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: BackpackService = TestBed.get(BackpackService);
    expect(service).toBeTruthy();
  });
});
