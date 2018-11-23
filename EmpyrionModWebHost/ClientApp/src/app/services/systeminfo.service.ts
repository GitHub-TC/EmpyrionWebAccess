import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { map } from 'rxjs/operators'
import { AuthenticationService } from '../_services';
import { User } from '../_models';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

@Injectable({
  providedIn: 'root'
})
export class SystemInfoService {
  public hubConnection: HubConnection;
  private LastSystemUpdateTime: Date = new Date();

  private mCurrentSystemInfo: SystemInfoModel = SYSTEMINFO;

  private SystemInfos: BehaviorSubject<SystemInfoModel> = new BehaviorSubject(this.mCurrentSystemInfo);
  public readonly SystemInfosObservable: Observable<SystemInfoModel> = this.SystemInfos.asObservable();
  error: any;

  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/systeminfo').build();

    // message coming from the server
    this.hubConnection.on("Update", D => {
      this.LastSystemUpdateTime = new Date();
      this.SystemInfos.next(this.mCurrentSystemInfo = JSON.parse(D));
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    interval(1000).pipe().subscribe(() => {
      if (this.mCurrentSystemInfo.online && (new Date().getTime() - this.LastSystemUpdateTime.getTime()) > 10000) {
        this.mCurrentSystemInfo.online = false;
        this.SystemInfos.next(this.mCurrentSystemInfo);
      }
    });
  }

  GetSystemInfos(): Observable<SystemInfoModel> {
    return this.SystemInfosObservable;
  }

  get CurrentSystemInfo() {
    return this.mCurrentSystemInfo;
  }
}
