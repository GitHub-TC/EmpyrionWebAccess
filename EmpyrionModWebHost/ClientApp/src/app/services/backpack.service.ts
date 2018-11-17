import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { BackpackModel } from '../model/backpack-model';
import { BACKPACKs } from '../model/backpack-mock';

@Injectable({
  providedIn: 'root'
})
export class BackpackService {
  public hubConnection: HubConnection;

  private backpacks: BackpackModel[] = BACKPACKs;

  constructor() {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/backpack').build();

    // message coming from the server
    this.hubConnection.on("PlayerUpdate", (message) => {
      //this.players.next(this.players.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    this.hubConnection.start();
  }

  GetBackpack(aEntityPlayerId: number): BackpackModel {
    return this.backpacks.find(B => B.entityPlayerId == aEntityPlayerId);
  }

}
