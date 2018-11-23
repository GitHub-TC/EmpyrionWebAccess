import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

import { FactionModel } from '../model/faction-model';
import { FACTION } from '../model/faction-mock';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

@Injectable({
  providedIn: 'root'
})
export class FactionService {
  public hubConnection: HubConnection;

  private factions: BehaviorSubject<FactionModel[]> = new BehaviorSubject(FACTION);
  public readonly factionsObservable: Observable<FactionModel[]> = this.factions.asObservable();
    error: any;

  constructor(private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/faction').build();

    // message coming from the server
    this.hubConnection.on("FactionUpdate", (message) => {
      //this.players.next(this.players.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  GetFaction(aId: number): FactionModel {
    return this.factions.getValue().find(F => F.factionId == aId);
  }

  GetPlayers(): Observable<FactionModel[]> {
    return this.factionsObservable;
  }
}
