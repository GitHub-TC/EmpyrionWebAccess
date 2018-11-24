import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { HubConnectionBuilder, HubConnection } from '@aspnet/signalr';
import { map } from 'rxjs/operators'

import { PlayerModel } from '../model/player-model';

import { PLAYER } from '../model/player-mock';
import { AuthenticationService } from '../services/authentication.service';
import { User } from '../model/user';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {
  public hubConnection: HubConnection;

  private mPlayers: PlayerModel[] = []; // PLAYER;

  private players: BehaviorSubject<PlayerModel[]> = new BehaviorSubject(this.mPlayers);
  public readonly playersObservable: Observable<PlayerModel[]> = this.players.asObservable();
  mCurrentPlayer: PlayerModel;
  error: any;

  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/player').build();

    // message coming from the server
    this.hubConnection.on("UpdatePlayer",  D => this.UpdatePlayersData([JSON.parse(D)]));
    this.hubConnection.on("UpdatePlayers", D => this.UpdatePlayersData( JSON.parse(D)));

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  private UpdatePlayersData(players: PlayerModel[]) {
    players.map(P => {
      let playerfound = this.mPlayers.findIndex(T => P.SteamId == T.SteamId);
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
      .pipe(map(S => S.value))
      .subscribe(
        P => this.players.next(this.mPlayers = P),
        error => this.error = error // error path
      );
    
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
