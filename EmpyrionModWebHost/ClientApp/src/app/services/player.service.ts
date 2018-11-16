import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { HubConnectionBuilder, HubConnection } from '@aspnet/signalr';

import { PlayerModel } from '../model/player-model';

import { PLAYER } from '../model/player-mock';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {
  public hubConnection: HubConnection;

  private players: BehaviorSubject<PlayerModel[]> = new BehaviorSubject(PLAYER);
  public readonly playersObservable: Observable<PlayerModel[]> = this.players.asObservable();

  constructor() {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/player').build();

    // message coming from the server
    this.hubConnection.on("PlayerUpdate", (message) => {
      //this.players.next(this.players.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    this.hubConnection.start();
  }

  GetPlayers(): Observable<PlayerModel[]> {
    return this.playersObservable;
  }

}
