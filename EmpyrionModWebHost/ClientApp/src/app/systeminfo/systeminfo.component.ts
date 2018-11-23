import { Component, OnInit } from '@angular/core';

import { SystemInfoService } from '../services/systeminfo.service';

import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';

@Component({
  selector: 'app-systeminfo',
  templateUrl: './systeminfo.component.html',
  styleUrls: ['./systeminfo.component.less']
})
export class SysteminfoComponent implements OnInit {
  CurrentSystemInfo: SystemInfoModel = SYSTEMINFO;

  constructor(private mSystemInfoService: SystemInfoService) { }

  ngOnInit() {
    this.mSystemInfoService.GetSystemInfos().subscribe(S => {
      this.CurrentSystemInfo = S;
    });
  }

}
