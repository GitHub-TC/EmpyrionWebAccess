import { Injectable, OnInit } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { map } from 'rxjs/operators'

import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

import { PlayerModel, PlayerInfoSet, ElevatedUserStruct, BannedUserStruct } from '../model/player-model';
import { ODataResponse } from '../model/ODataResponse';
import { PLAYER } from '../model/player-mock';
import { PlayfieldService } from './playfield.service';

@Injectable({
  providedIn: 'root'
})
export class PlayerService {
  public hubConnection: HubConnection;

  private mPlayers: PlayerModel[] = []; // PLAYER;

  private players: BehaviorSubject<PlayerModel[]> = new BehaviorSubject(this.mPlayers);
  public readonly playersObservable: Observable<PlayerModel[]> = this.players.asObservable();
  mCurrentPlayer: PlayerModel = null;

  private currentPlayer: BehaviorSubject<PlayerModel> = new BehaviorSubject(this.mCurrentPlayer);
  public readonly currentPlayerObservable: Observable<PlayerModel> = this.currentPlayer.asObservable();

  error: any;
  public ElevatedUser: ElevatedUserStruct[] = [];
  public BannedUser: BannedUserStruct[] = [];

  constructor(
    private http: HttpClient,
    private builder: AuthHubConnectionBuilder,
    private mPlayfields: PlayfieldService,
  ) {
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

  ReadPlayers(): any {
    let locationsSubscription = this.http.get<ODataResponse<PlayerModel[]>>("odata/Players")
      .pipe(map(S => S.value))
      .subscribe(
        P => this.players.next(this.mPlayers = P.map(p => this.CorrectPlayer(p))),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    let locationsSubscription2 = this.http.get<ElevatedUserStruct[]>("Player/GetElevatedUsers")
      .pipe()
      .subscribe(
        P => this.ElevatedUser = P ? P : [],
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription2.unsubscribe(); }, 120000);

    let locationsSubscription3 = this.http.get<BannedUserStruct[]>("Player/GetBannedUsers")
      .pipe()
      .subscribe(
        P => this.BannedUser = P
          ? P.map(p => {
                    try { p.until = new Date(p.until); } catch { }
                    return p;
          })
          : []
          ,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription3.unsubscribe(); }, 120000);

  }

  CorrectPlayer(aPlayer: PlayerModel) {
    try { aPlayer.LastOnline = new Date(aPlayer.LastOnline); } catch { }
    aPlayer.Pos = { x: aPlayer.PosX, y: aPlayer.PosY, z: aPlayer.PosZ };
    aPlayer.Rot = { x: aPlayer.RotX, y: aPlayer.RotY, z: aPlayer.RotZ };
    return aPlayer;
  }

  private UpdatePlayersData(players: PlayerModel[]) {
    players.map(P => {
      P = this.CorrectPlayer(P);
      this.mPlayfields.UpdatePlayfield(P.Playfield);
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
    if (!this.mPlayers || !this.mPlayers.length) this.ReadPlayers();
    return this.playersObservable;
  }

  saveUser(aPlayer: PlayerModel): any {
    this.http.post<PlayerInfoSet>('Player/ChangePlayerInfo', {
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

  saveNote(aPlayer: PlayerModel): any {
    this.http.post('Player/SaveNote', {
      SteamId: aPlayer.SteamId,
      Note: aPlayer.Note
    })
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

  executeCommand(aPlayer: PlayerModel, command: string): any {
    this.http.post('Player/PlayerRemoteEx', {
      ClientId: aPlayer.ClientId,
      Command: command
    })
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

  KickPlayer(aPlayer: PlayerModel): any {
    this.http.get('gameplay/KickPlayer/' + aPlayer.SteamId)
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

  BanPlayer(aPlayer: PlayerModel, aDuration: string): any {
    this.http.get('gameplay/BanPlayer/' + aPlayer.SteamId + "/" + aDuration)
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

  SetRoleOfPlayer(player: PlayerModel, role: string) {
    this.http.get('gameplay/SetRoleOfPlayer/' + player.SteamId + '/' + role)
      .pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );
  }

}
