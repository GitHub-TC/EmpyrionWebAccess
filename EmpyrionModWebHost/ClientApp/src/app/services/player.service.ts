import { Injectable, OnInit } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { map } from 'rxjs/operators'

import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

import { PlayerModel, PlayerInfoSet } from '../model/player-model';
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

  private currentPlayer: BehaviorSubject<PlayerModel> = new BehaviorSubject(this.mCurrentPlayer);
  public readonly currentPlayerObservable: Observable<PlayerModel> = this.currentPlayer.asObservable();

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

  CorrectDateTimes(aPlayer: PlayerModel) {
    try { aPlayer.LastOnline = new Date(aPlayer.LastOnline); } catch {}
    try {
      aPlayer.OnlineTime = aPlayer.OnlineTime.substr(0, aPlayer.OnlineTime.lastIndexOf('.'));
      aPlayer.OnlineTime = aPlayer.OnlineTime.replace("PT", "");
      aPlayer.OnlineTime = aPlayer.OnlineTime.replace("H", ":");
      aPlayer.OnlineTime = aPlayer.OnlineTime.replace("M", ":");
    } catch { }
    return aPlayer;
  }

  private UpdatePlayersData(players: PlayerModel[]) {
    players.map(P => {
      P = this.CorrectDateTimes(P);
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
        P => this.players.next(this.mPlayers = P.map(p => this.CorrectDateTimes(p))),
        error => this.error = error // error path
    );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

    return this.playersObservable;
  }

  saveUser(aPlayer: PlayerModel): any {
    this.http.post<PlayerInfoSet>('odata/Players', {
      entityId: aPlayer.EntityId,
      factionRole: aPlayer.FactionRole,
      factionId: aPlayer.FactionId,
      factionGroup: aPlayer.FactionGroup,
      origin: aPlayer.Origin,
      upgradePoints: Math.floor(aPlayer.Upgrade),
      experiencePoints: Math.floor(aPlayer.Exp),
      bodyTempMax: Math.floor(aPlayer.BodyTempMax),
      bodyTemp: Math.floor(aPlayer.BodyTemp),
      oxygenMax: Math.floor(aPlayer.OxygenMax),
      oxygen: Math.floor(aPlayer.Oxygen),
      foodMax: Math.floor(aPlayer.FoodMax),
      food: Math.floor(aPlayer.Food),
      staminaMax: Math.floor(aPlayer.StaminaMax),
      stamina: Math.floor(aPlayer.Stamina),
      healthMax: Math.floor(aPlayer.HealthMax),
      health: Math.floor(aPlayer.Health),
      startPlayfield: aPlayer.StartPlayfield,
      radiationMax: Math.floor(aPlayer.RadiationMax),
      radiation: Math.floor(aPlayer.Radiation),
    })
      .pipe()
      .subscribe(
      () => { },
        error => this.error = error // error path
    );

    this.http.post('gameplay/PlayerSetCredits/' + aPlayer.EntityId + "/" + Math.floor(aPlayer.Credits), {})
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

  GetPlayer(aSelect: (PlayerModel) => boolean) {
    return this.players.getValue().find(aSelect);
  }

  GetCurrentPlayer() {
    return this.currentPlayerObservable;
  }

  get CurrentPlayer() {
    return this.mCurrentPlayer;
  }

  set CurrentPlayer(aPlayer: PlayerModel) {
    this.currentPlayer.next(this.mCurrentPlayer = aPlayer);
  }

  BanPlayer(aPlayer: PlayerModel): any {
    this.http.get('gameplay/BanPlayer/' + aPlayer.SteamId + "/" + "12m")
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

  UnBanPlayer(aPlayer: PlayerModel): any {
    this.http.get('gameplay/UnBanPlayer/' + aPlayer.SteamId)
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

  WipePlayer(aPlayer: PlayerModel): any {
    this.http.get('gameplay/WipePlayer/' + aPlayer.SteamId)
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

}
