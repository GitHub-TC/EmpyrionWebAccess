import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { map } from 'rxjs/operators'
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';
import { Router } from '@angular/router';

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
      this.SystemInfos.next(this.mCurrentSystemInfo = JSON.parse(D));
    });
    this.hubConnection.on("UPC", D => {
      this.LastSystemUpdateTime = new Date();
      let perf = JSON.parse(D);
      if (perf.o  ) this.mCurrentSystemInfo.online = perf.o;
      if (perf.ap ) this.mCurrentSystemInfo.activePlayers = perf.ap;
      if (perf.apf) this.mCurrentSystemInfo.activePlayfields = perf.apf;
      if (perf.c  ) this.mCurrentSystemInfo.cpuTotalLoad = perf.c;
      if (perf.r  ) this.mCurrentSystemInfo.ramAvailableMB = perf.r;
      if (perf.tpf) this.mCurrentSystemInfo.totalPlayfieldserver = perf.tpf;
      if (perf.tpfm) this.mCurrentSystemInfo.totalPlayfieldserverRamMB = perf.tpfm;
      this.SystemInfos.next(this.mCurrentSystemInfo);
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    interval(1000).pipe().subscribe(() => {
      if (this.mCurrentSystemInfo.online && (new Date().getTime() - this.LastSystemUpdateTime.getTime()) > 10000) {
        this.mCurrentSystemInfo.online = "D";
        this.SystemInfos.next(this.mCurrentSystemInfo);
        if((new Date().getTime() - this.LastSystemUpdateTime.getTime()) > 60000) this.TestIfOnlineAgain();
      }
    });

    let locationsSubscription = this.http.get<SystemInfoModel>("systeminfo/CurrentSysteminfo")
      .pipe()
      .subscribe(
      I => {
        this.LastSystemUpdateTime = new Date();
        this.SystemInfos.next(this.mCurrentSystemInfo = I);
      },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

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
    return this.SystemInfosObservable;
  }

  get CurrentSystemInfo() {
    return this.mCurrentSystemInfo;
  }
}
