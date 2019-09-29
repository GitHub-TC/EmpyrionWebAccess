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
export class FactionService {
  public hubConnection: HubConnection;

  private mFactions: FactionModel[] = []; // PLAYER;

  private factions: BehaviorSubject<FactionModel[]> = new BehaviorSubject(this.mFactions);
  public readonly factionsObservable: Observable<FactionModel[]> = this.factions.asObservable();
  error: any;

  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/faction').build();

    // message coming from the server
    this.hubConnection.on("Update", F => this.factions.next(this.mFactions = JSON.parse(F).sort((a, b) => a.Abbrev < b.Abbrev ? 1 : a.Abbrev === b.Abbrev ? 0 : -1)));

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    this.ReadFactions();
  }

  ReadFactions(): any {
    let locationsSubscription = this.http.get<ODataResponse<FactionModel[]>>("odata/Factions")
      .pipe(map(S => S.value))
      .subscribe(
        F => this.factions.next(this.mFactions = F.sort((a, b) => a.Abbrev < b.Abbrev ? 1 : a.Abbrev === b.Abbrev ? 0 : -1)),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  GetFactions(): Observable<FactionModel[]> {
    return this.factionsObservable;
  }

  GetFaction(aId: number): FactionModel {
    return this.factions.getValue().find(F => F.FactionId == aId);
  }

  GetFactionGroup(factionGroup: number): string {
    switch (factionGroup) {
      case 0: return "Faction";
      case 1: return "Privat";
      case 2: return "Zirax";
      case 3: return "Predator";
      case 4: return "Prey";
      case 5: return "Admin";
      case 6: return "Talon";
      case 7: return "Polaris";
      case 8: return "Alien";
      case 11: return "Unknown";
      case 10: return "Public";
      case 12: return "None";
      case 255: return "Decored";
      default: return "" + factionGroup;
    }
  }

}
