import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators'

import { FactionModel } from '../model/faction-model';
import { FACTION } from '../model/faction-mock';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

@Injectable({
  providedIn: 'root'
})
export class FactionService implements OnInit {
  public hubConnection: HubConnection;

  private mFactions: FactionModel[] = []; // PLAYER;

  private factions: BehaviorSubject<FactionModel[]> = new BehaviorSubject(this.mFactions);
  public readonly factionsObservable: Observable<FactionModel[]> = this.factions.asObservable();
  error: any;

  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/faction').build();

    // message coming from the server
    this.hubConnection.on("Update", this.UpdateFactionData);

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  ngOnInit(): void {
    this.http.get<ODataResponse<FactionModel[]>>("odata/Factions")
      .pipe(map(S => S.value))
      .subscribe(
        F => this.factions.next(this.mFactions = F),
        error => this.error = error // error path
      );
  }

  private UpdateFactionData(aFaction: FactionModel) {
    let factionfound = this.mFactions.findIndex(T => aFaction.origin == T.origin);
    if (factionfound == -1)
      this.mFactions.push(aFaction);
    else {
      let newList = this.mFactions.slice(0, factionfound);
      newList.push(aFaction);
      newList = newList.concat(this.mFactions.slice(factionfound + 1));
      this.mFactions = newList;

      this.factions.next(this.mFactions);
    }
  }

  GetFactions(): Observable<FactionModel[]> {
    return this.factionsObservable;
  }


  GetFaction(aId: number): FactionModel {
    return this.factions.getValue().find(F => F.factionId == aId);
  }

  GetPlayers(): Observable<FactionModel[]> {
    return this.factionsObservable;
  }
}
