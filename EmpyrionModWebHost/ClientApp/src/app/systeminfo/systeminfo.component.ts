import { Component, OnInit, Output } from '@angular/core';

import { SystemInfoService } from '../services/systeminfo.service';

import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';
import { Router } from '@angular/router';
import { AuthenticationService } from '../services/authentication.service';
import { User } from '../model/user';

@Component({
  selector: 'app-systeminfo',
  templateUrl: './systeminfo.component.html',
  styleUrls: ['./systeminfo.component.less']
})
export class SysteminfoComponent implements OnInit {
  @Output() CurrentSystemInfo: SystemInfoModel = SYSTEMINFO;
  currentUser: User;

  constructor(
    public router: Router,
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

  GetStateClass() {
    if (!this.CurrentSystemInfo.online) return "";
    if (this.CurrentSystemInfo.online.indexOf("o") >= 0) return "online";
    if (this.CurrentSystemInfo.online.indexOf("b") >= 0) return "backup";
    if (this.CurrentSystemInfo.online.indexOf("r") >= 0) return "restart";
    if (this.CurrentSystemInfo.online.indexOf("S") >= 0) return "stopped";
    if (this.CurrentSystemInfo.online.indexOf("c") >= 0) return "egs_comm";
    if (this.CurrentSystemInfo.online.indexOf("E") >= 0) return "offline";
    if (this.CurrentSystemInfo.online.indexOf("D") >= 0) return "disconnect";
    return ""
  }

  GetStateDescription() {
    if (!this.CurrentSystemInfo.online) return "";
    if (this.CurrentSystemInfo.online.indexOf("o") >= 0) return "online";
    if (this.CurrentSystemInfo.online.indexOf("b") >= 0) return "backup";
    if (this.CurrentSystemInfo.online.indexOf("r") >= 0) return "restart";
    if (this.CurrentSystemInfo.online.indexOf("S") >= 0) return "stopped";
    if (this.CurrentSystemInfo.online.indexOf("c") >= 0) return "EGS_COM_Error";
    if (this.CurrentSystemInfo.online.indexOf("E") >= 0) return "offline";
    if (this.CurrentSystemInfo.online.indexOf("D") >= 0) return "disconnect";
    return ""
  }

  openHelp() {
    window.open("https://github.com/GitHub-TC/EmpyrionWebAccess/blob/master/Readme.md", "_blank");
  }

  latestVersion() {
    window.open("https://github.com/GitHub-TC/EmpyrionWebAccess/releases", "_blank");
  }
  
}
