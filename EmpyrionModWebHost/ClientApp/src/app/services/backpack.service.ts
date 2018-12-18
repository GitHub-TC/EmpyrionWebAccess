import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { BackpackModel, BackpackODataModel } from '../model/backpack-model';
import { BACKPACKs } from '../model/backpack-mock';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';


@Injectable({
  providedIn: 'root'
})
export class BackpackService {
  public hubConnection: HubConnection;

  private mBackpack: BackpackModel = {};

  private backpack: BehaviorSubject<BackpackModel> = new BehaviorSubject(this.mBackpack);
  public readonly backpackObservable: Observable<BackpackModel> = this.backpack.asObservable();
    error: any;

  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/backpack').build();
    this.hubConnection.on("UpdateBackpack", D => {
      let Data = JSON.parse(D);
      this.UpdateBackpackData({ id: Data.Id, toolbarContent: Data.ToolbarContent, bagContent: Data.BagContent});
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  private UpdateBackpackData(B: BackpackODataModel) {
    if (B.id != this.mBackpack.SteamId) return;
    this.mBackpack.Toolbar = B && B.toolbarContent ? JSON.parse(B.toolbarContent) : [];
    this.mBackpack.Bag     = B && B.bagContent     ? JSON.parse(B.bagContent)     : [];
    this.backpack.next(this.mBackpack);
  }

  GetBackpack(aPlayerSteamId: string): Observable<BackpackModel> {
    this.mBackpack = { SteamId: aPlayerSteamId, Toolbar:[], Bag: [] };
    this.backpack.next(this.mBackpack);

    if (aPlayerSteamId) {
      let locationsSubscription = this.http.get<BackpackODataModel>("backpacks/CurrentBackpack/" + aPlayerSteamId)
        .subscribe(
          B => this.UpdateBackpackData(B),
          error => this.error = error // error path
      );
      // Stop listening for location after 10 seconds
      setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
    }

    return this.backpackObservable;
  }

}
