import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { SystemInfoModel } from '../model/systeminfo-model';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';
import { Router } from '@angular/router';
import { SystemConfig } from '../model/systemconfig-model';

@Injectable({
  providedIn: 'root'
})
export class SystemInfoService {
  public hubConnection: HubConnection;
  private LastSystemUpdateTime: Date = new Date();

  private mCurrentSystemInfo: SystemInfoModel = {};// SYSTEMINFO;

  private SystemInfos: BehaviorSubject<SystemInfoModel> = new BehaviorSubject(this.mCurrentSystemInfo);
  public readonly SystemInfosObservable: Observable<SystemInfoModel> = this.SystemInfos.asObservable();
  error: any;
  firstRead: boolean = true;
  SystemConfig: SystemConfig = { playerSteamInfoUrl: "https://steamcommunity.com/profiles" };

  constructor(
    public router: Router,
    private http: HttpClient,
    private builder: AuthHubConnectionBuilder
  ) {
    this.hubConnection = builder.withAuthUrl('/hubs/systeminfo').build();
    this.hubConnection.onclose(E => console.log("!!!! HubClosed:" + E));

    // message coming from the server
    this.hubConnection.on("Update", D => {
      this.LastSystemUpdateTime = new Date();
      this.CurrentSystemInfo = JSON.parse(D);
    });
    this.hubConnection.on("UPC", D => {
      this.LastSystemUpdateTime = new Date();
      let perf = JSON.parse(D);
      if (perf.o   ) this.mCurrentSystemInfo.online                         = perf.o;
      if (perf.ap  ) this.mCurrentSystemInfo.activePlayers                  = perf.ap;
      if (perf.apf ) this.mCurrentSystemInfo.activePlayfields               = perf.apf;
      if (perf.c   ) this.mCurrentSystemInfo.cpuTotalLoad                   = perf.c;
      if (perf.r   ) this.mCurrentSystemInfo.ramAvailableMB                 = perf.r;
      if (perf.tpf ) this.mCurrentSystemInfo.totalPlayfieldserver           = perf.tpf;
      if (perf.tpfm) this.mCurrentSystemInfo.totalPlayfieldserverMemorySize = perf.tpfm;
      if (perf.ewam) this.mCurrentSystemInfo.ewaMemorySize                  = perf.ewam;
      if (perf.eahm) this.mCurrentSystemInfo.eahMemorySize                  = perf.eahm;
      this.CurrentSystemInfo                                                = this.mCurrentSystemInfo;
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    interval(5000).pipe().subscribe(() => {
      if ((new Date().getTime() - this.LastSystemUpdateTime.getTime()) > 30000) {
        this.mCurrentSystemInfo.online = "D";
        this.CurrentSystemInfo = this.mCurrentSystemInfo;
        if((new Date().getTime() - this.LastSystemUpdateTime.getTime()) > 120000) this.TestIfOnlineAgain();
      }
    });

    this.http.get<SystemConfig>("systeminfo/SystemConfig")
      .pipe()
      .subscribe(
        S => this.SystemConfig = S,
        error => this.error = error // error path
      );

    this.LastSystemUpdateTime = new Date();
  }

  TestIfOnlineAgain(): any {
    let locationsSubscription = this.http.get<SystemInfoModel>("systeminfo/CurrentSysteminfo")
      .pipe()
      .subscribe(
        I => window.location.reload(),
        error => this.error = error // error path
      );
    // Stop listening for location after 500ms
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 500);
  }

  GetSystemInfos(): Observable<SystemInfoModel> {
    this.CheckAndReadFirstData();
    return this.SystemInfosObservable;
  }

  get CurrentSystemInfo() {
    this.CheckAndReadFirstData();
    return this.mCurrentSystemInfo;
  }

  private CheckAndReadFirstData() {
    if (!this.firstRead) return;
    this.firstRead = false;

    let locationsSubscription = this.http.get<SystemInfoModel>("systeminfo/CurrentSysteminfo")
        .pipe()
        .subscribe(I => {
            this.LastSystemUpdateTime = new Date();
            this.CurrentSystemInfo = I;
        }, error => this.error = error // error path
        );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  set CurrentSystemInfo(nextInfo: SystemInfoModel) {
    this.SystemInfos.next(this.mCurrentSystemInfo = nextInfo);
  }

}
