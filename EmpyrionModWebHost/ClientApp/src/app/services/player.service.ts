import { Injectable, OnInit } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { map } from 'rxjs/operators'

import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

import { PlayerModel } from '../model/player-model';
import { PLAYER } from '../model/player-mock';

@Injectable({
  providedIn: 'root'
})
export class PlayerService implements OnInit {
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

  ngOnInit(): void {
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
    let locationsSubscription = this.http.get<ODataResponse<PlayerModel[]>>("odata/Players?$orderby=PlayerName asc")
      .pipe(map(S => S.value))
      .subscribe(
        P => this.players.next(this.mPlayers = P),
        error => this.error = error // error path
    );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

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
