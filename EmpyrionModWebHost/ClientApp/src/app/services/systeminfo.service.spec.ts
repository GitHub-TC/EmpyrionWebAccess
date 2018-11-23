import { TestBed } from '@angular/core/testing';

import { SystemInfoService } from './systeminfo.service';

describe('SysteminfoService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: SystemInfoService = TestBed.get(SystemInfoService);
    expect(service).toBeTruthy();
  });
});
