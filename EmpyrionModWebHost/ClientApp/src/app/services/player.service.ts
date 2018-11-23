import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { HubConnectionBuilder, HubConnection } from '@aspnet/signalr';
import 'rxjs/add/operator/map';

import { PlayerModel } from '../model/player-model';

import { PLAYER } from '../model/player-mock';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {
  public hubConnection: HubConnection;

  private mPlayers: PlayerModel[] = PLAYER;

  private players: BehaviorSubject<PlayerModel[]> = new BehaviorSubject(this.mPlayers);
  public readonly playersObservable: Observable<PlayerModel[]> = this.players.asObservable();
  mCurrentPlayer: PlayerModel;

  constructor(private http: HttpClient) {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/player').build();

    // message coming from the server
    this.hubConnection.on("UpdatePlayer",  D => this.UpdatePlayersData([JSON.parse(D)]));
    this.hubConnection.on("UpdatePlayers", D => this.UpdatePlayersData( JSON.parse(D)));

    // starting the connection
    this.hubConnection.start();
  }

  private UpdatePlayersData(players: PlayerModel[]) {
    players.map(P => {
      let playerfound = this.mPlayers.findIndex(P => P.steamId == P.steamId);
      if (playerfound == -1)
        this.mPlayers.push(P);
      else {
        let newList = this.mPlayers.slice(0, playerfound);
        newList.push(P);
        newList = newList.concat(this.mPlayers.slice(playerfound + 1));
        this.mPlayers = newList;
      };
    });

    this.players.next(this.mPlayers);
  }

  GetPlayers(): Observable<PlayerModel[]> {
    this.http.get<ODataResponse<PlayerModel[]>>("odata/Players")
      .map(S => S.value)
      .subscribe(P => this.players.next(this.mPlayers = P));

    return this.playersObservable;
  }

  GetPlayer(aSelect: (PlayerModel) => boolean) {
    return this.players.getValue().find(aSelect);
  }

  get CurrentPlayer() {
    return this.mCurrentPlayer;
  }

  set CurrentPlayer(aPlayer: PlayerModel) {
    this.mCurrentPlayer = aPlayer;
  }
}
