import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

class ProcessInformation {
  id?: number
  currentDirecrory?: string;
  arguments?: string;
  fileName?: string;
}

class SystemConfig {
  processInformation?: ProcessInformation;
  startCMD?: string;
}

@Component({
  selector: 'app-server-settings',
  templateUrl: './server-settings.component.html',
  styleUrls: ['./server-settings.component.less']
})
export class ServerSettingsComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  StartCMDList: string[] = [];
  WaitMinutes: number = 0;
  error: any;
  SystemConfig: SystemConfig = {};

  constructor(private http: HttpClient) { }

  ngOnInit() {
    this.http.get<string[]>("systeminfo/StartCMDs")
      .pipe()
      .subscribe(
        S => this.StartCMDList = S,
        error => this.error = error // error path
      );

    this.http.get<SystemConfig>("systeminfo/SystemConfig")
      .pipe()
      .subscribe(
        S => this.SystemConfig = S,
        error => this.error = error // error path
      );
  }

  Save() {
    this.http.post<SystemConfig>("systeminfo/SystemConfig", this.SystemConfig)
      .pipe()
      .subscribe(
        S => { },
        error => this.error = error // error path
      );
  }

  StartEGS() {
    this.YesNo.openDialog({ title: "Empyrion Gameserver", question: "start" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get<SystemConfig>("systeminfo/EGSStart")
          .pipe()
          .subscribe(
            S => { },
            error => this.error = error // error path
          );
      });
  }

  StopEGS() {
    this.YesNo.openDialog({ title: "Empyrion Gameserver", question: "stop in " + this.WaitMinutes + " minutes"}).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get<SystemConfig>("systeminfo/EGSStop/" + this.WaitMinutes)
          .pipe()
          .subscribe(
            S => { },
            error => this.error = error // error path
          );
      });
  }

  RestartEGS() {
    this.YesNo.openDialog({ title: "Empyrion Gameserver", question: "restart in " + this.WaitMinutes + " minutes" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get<SystemConfig>("systeminfo/EGSRestart/" + this.WaitMinutes)
          .pipe()
          .subscribe(
            S => { },
            error => this.error = error // error path
          );
      });
  }

}
