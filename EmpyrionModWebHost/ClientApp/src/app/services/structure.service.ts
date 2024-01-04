import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubConnection } from '@aspnet/signalr';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { AuthHubConnectionBuilder } from '../_helpers';
import { GlobalStructureInfo } from '../model/structure-model';
import { FactionService } from './faction.service';
import { PlayerService } from './player.service';
import { PlayerModel } from '../model/player-model';
import { catchError, timeout } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class StructureService {
  public hubConnection: HubConnection;

  mPlayers: PlayerModel[] = [];
  private mStructures: GlobalStructureInfo[] = [];
  public FilterPreset: string;
  public WithPOIs: boolean = false;

  private Structures: BehaviorSubject<GlobalStructureInfo[]> = new BehaviorSubject(this.mStructures);
  public readonly StructuresObservable: Observable<GlobalStructureInfo[]> = this.Structures.asObservable();

  mCurrentStructure: GlobalStructureInfo = null;

  private currentStructure: BehaviorSubject<GlobalStructureInfo> = new BehaviorSubject(this.mCurrentStructure);
  public readonly currentStructureObservable: Observable<GlobalStructureInfo> = this.currentStructure.asObservable();

  error: any;

  constructor(
    private http: HttpClient,
    private mFactionService: FactionService,
    private mPlayerService: PlayerService,
    private builder: AuthHubConnectionBuilder
  ) {
    this.hubConnection = builder.withAuthUrl('/hubs/structures').build();

    // message coming from the server
    this.hubConnection.on("Update", D => this.UpdateStructureData([JSON.parse(D)]));

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    this.mPlayerService.GetPlayers().subscribe(P => this.mPlayers = P);
  }

  GetCurrentStructure() {
    return this.currentStructureObservable;
  }

  public get CurrentStructure() {
    return this.mCurrentStructure;
  }

  public set CurrentStructure(aStructure: GlobalStructureInfo) {
    this.currentStructure.next(this.mCurrentStructure = aStructure);
  }


  GetGlobalStructureList(): Observable<GlobalStructureInfo[]> {
    if (!this.mStructures.length) this.ReloadStructures();
    return this.StructuresObservable;
  }

  UpdateStructureData(arg0: any[]): void {
  }

  public ReloadStructures() {
    let locationsSubscription = this.http.get<any>("Structure/GlobalStructureList?WithPOIs=" + this.WithPOIs, { headers: new HttpHeaders({ timeout: `${300000}` }) })
      .subscribe(
        S => {
          this.mStructures = S.map((S: GlobalStructureInfo) => {
              S.playfield       = S.playfieldName;
              S.solarSystemName = S.solarSystemName;
              S.CoreName = ["None", "Player", "Admin", "Alien", "AlienAdmin", "NoFaction"][S.coreType];
              S.TypeName = ["Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel"][S.type];
              if (S.factionGroup == 1) {
                let Found = this.mPlayers.find(P => P.EntityId == S.factionId);
                S.FactionName = Found ? Found.PlayerName : "" + S.factionId;
              }
              else {
                let Faction = this.mFactionService.GetFaction(S.factionId);
                S.FactionName = Faction ? Faction.Abbrev : "";
              }
              S.FactionGroup = this.mFactionService.GetFactionGroup(S.factionGroup);
              return S;
            });
          this.Structures.next(this.mStructures)
        },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 300000);
  }

}
