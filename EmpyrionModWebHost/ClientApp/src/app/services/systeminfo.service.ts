import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { SystemInfoModel } from '../model/systeminfo-model';
import { SYSTEMINFO } from '../model/systeminfo-mock';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SystemInfoService {
  public hubConnection: HubConnection;

  private mCurrentSystemInfo: SystemInfoModel = SYSTEMINFO;

  private SystemInfos: BehaviorSubject<SystemInfoModel> = new BehaviorSubject(this.mCurrentSystemInfo);
  public readonly SystemInfosObservable: Observable<SystemInfoModel> = this.SystemInfos.asObservable();

  constructor(private http: HttpClient) {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/systeminfo').build();

    // message coming from the server
    this.hubConnection.on("UpdateSystemInfo", D => this.SystemInfos.next(this.mCurrentSystemInfo = JSON.parse(D)));

    // starting the connection
    this.hubConnection.start();
  }

  GetSystemInfos(): Observable<SystemInfoModel> {
    this.http.get<SystemInfoModel>("odata/SystemInfos")
      .subscribe(S => this.SystemInfos.next(this.mCurrentSystemInfo = S));

    return this.SystemInfosObservable;
  }

  get CurrentSystemInfo() {
    return this.mCurrentSystemInfo;
  }
}
