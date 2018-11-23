import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { BackpackModel } from '../model/backpack-model';
import { BACKPACKs } from '../model/backpack-mock';


interface BackpackODataModel {
  Id?: string;
  Timestamp?: string;
  Content?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BackpackService {
  public hubConnection: HubConnection;

  private mBackpack: BackpackModel = {};

  private backpack: BehaviorSubject<BackpackModel> = new BehaviorSubject(this.mBackpack);
  public readonly backpackObservable: Observable<BackpackModel> = this.backpack.asObservable();

  constructor(private http: HttpClient) {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/backpack').build();

    // message coming from the server
    this.hubConnection.on("UpdateBackpack", D => this.UpdateBackpackData(JSON.parse(D)));

    // starting the connection
    this.hubConnection.start();
  }

  private UpdateBackpackData(backpack: BackpackODataModel) {
    if (backpack.Id != this.mBackpack.steamId) return;
    this.mBackpack.backpack = JSON.parse(backpack.Content);
    this.backpack.next(this.mBackpack);
  }

  GetBackpack(aPlayerSteamId: string): Observable<BackpackModel> {
    this.mBackpack = { steamId: aPlayerSteamId, backpack: [] };
    this.backpack.next(this.mBackpack);

    if (aPlayerSteamId) this.http.get<BackpackODataModel>("odata/Backpacks('" + aPlayerSteamId + "')")
      .subscribe(B => {
        this.mBackpack.backpack = JSON.parse(B.Content);
        this.backpack.next(this.mBackpack);
      });

    return this.backpackObservable;
  }

}
