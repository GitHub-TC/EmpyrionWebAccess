import { Component, OnInit } from '@angular/core';

import { SystemInfoService } from '../services/systeminfo.service';

import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';
import { AuthenticationService } from '../_services';
import { Router } from '@angular/router';
import { User } from '../_models';

@Component({
  selector: 'app-systeminfo',
  templateUrl: './systeminfo.component.html',
  styleUrls: ['./systeminfo.component.less']
})
export class SysteminfoComponent implements OnInit {
  CurrentSystemInfo: SystemInfoModel = SYSTEMINFO;
  currentUser: User;

  constructor(
    private router: Router,
    private authenticationService: AuthenticationService,
    private mSystemInfoService: SystemInfoService) {
      this.authenticationService.currentUser.subscribe(x => this.currentUser = x);
  }

  ngOnInit() {
    this.mSystemInfoService.GetSystemInfos().subscribe(S => {
      this.CurrentSystemInfo = S;
    });
  }

  logout() {
    this.authenticationService.logout();
    this.router.navigate(['/login']);
  }

}
