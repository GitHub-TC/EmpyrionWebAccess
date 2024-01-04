import { Component, OnInit, Input, ViewChild } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';
import { ChatService } from '../services/chat.service';
import { PositionService } from '../services/position.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { PLAYER } from '../model/player-mock';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';
import { OriginService } from '../services/origin.service';
import { Router } from '@angular/router';
import { StructureService } from '../services/structure.service';
import { SystemInfoService } from '../services/systeminfo.service';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['Online', 'PlayerName', 'Faction', 'Playfield', 'PosX', 'PosY', 'PosZ', 'SolarSystem', 'Origin', 'LastOnline', 'OnlineHours', 'EntityId', 'SteamId'];
  players: MatTableDataSource<PlayerModel> = new MatTableDataSource([]);
  displayFilter: boolean;

  @ViewChild(MatSort, { static: true }) sort: MatSort;

  message: string;
  autoscroll: boolean = true;
  mFactions: FactionModel[] = [];
  mOrigins: { [id: number]: string; } = [];
  mSelectedPlayfield: string;
  mAllPlayers: PlayerModel[];
  UserRole = UserRole;

  constructor(
    public router: Router,
    private mStructureService: StructureService,
    private mFactionService: FactionService,
    private mPlayerService: PlayerService,
    private mPositionService: PositionService,
    private mChatService: ChatService,
    private mOriginService: OriginService,
    public mSystemInfoService: SystemInfoService,
    public role: RoleService,
  ) {
  }

  ngOnInit() {
    this.mPlayerService.GetPlayers().subscribe(players => {
      setTimeout(() => {
        this.mAllPlayers = players;
        this.SelectedPlayfield = this.mSelectedPlayfield;
      }, 10);
    });
    this.mFactionService.GetFactions().subscribe(F => this.mFactions = F);
    this.mOriginService.GetOrigins().subscribe(O => this.mOrigins = O);
  }

  ngAfterViewInit() {
    this.players.sort = this.sort;
    this.players.sortingDataAccessor = (D, H) => typeof (D[H]) === "string" ? ("" + D[H]).toLowerCase() + D["PlayerName"].toLowerCase() : (typeof (D[H]) === "boolean" ? !D[H] + D["PlayerName"].toLowerCase() : D[H]);
    this.players.filterPredicate =
      (data: PlayerModel, filter: string) =>
        data.PlayerName                   .trim().toLowerCase().indexOf(filter) != -1 ||
        data.Playfield                    .trim().toLowerCase().indexOf(filter) != -1 ||
        data.SolarSystem                  .trim().toLowerCase().indexOf(filter) != -1 ||
        data.EntityId                     .toString()          .indexOf(filter) != -1 ||
        data.SteamId                                           .indexOf(filter) != -1 ||
        this.Origin(data)                 .trim().toLowerCase().indexOf(filter) != -1 ||
        this.Faction(data) && this.Faction(data).Abbrev.trim().toLowerCase().indexOf(filter) != -1 ||
        ('' + data.FactionId).indexOf(filter) != -1;
  }

  @Input()
  set SelectedPlayfield(aPlayfield: string) {
    this.mSelectedPlayfield = aPlayfield;

    if (this.mAllPlayers) this.players.data = this.mAllPlayers.filter(p => !this.mSelectedPlayfield || p.Playfield == this.mSelectedPlayfield)
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.players.filter = filterValue;
  }

  toggleFilterDisplay(FilterInput) {
    this.displayFilter = !this.displayFilter;
    if (this.displayFilter) setTimeout(() => FilterInput.focus(), 0);
  }

  get CurrentPlayerSteamId() {
    return this.mPlayerService.CurrentPlayer ? this.mPlayerService.CurrentPlayer.SteamId : 0;
  }

  get CurrentPlayer() {
    return this.mPlayerService.CurrentPlayer;
  }

  @Input() set CurrentPlayer(aPlayer: PlayerModel) {
    this.mPlayerService.CurrentPlayer = aPlayer;
  }

  SavePosition(aPlayer: PlayerModel) {
    this.mPositionService.CurrentPosition = {
      description: "Player: " + aPlayer.PlayerName,
      entityId: aPlayer.EntityId,
      playfield: aPlayer.Playfield,
      pos: { x: aPlayer.PosX, y: aPlayer.PosY, z: aPlayer.PosZ },
      rot: { x: aPlayer.RotX, y: aPlayer.RotY, z: aPlayer.RotZ }
    };
  }

  ChatToPlayer(aPlayer: PlayerModel) {
    this.mChatService.ChatToPlayer(aPlayer);
  }

  ChatToFaction(aPlayer: PlayerModel) {
    this.mChatService.ChatToFaction(this.mFactions.find(F => F.FactionId == aPlayer.FactionId));
  }

  Faction(aPlayer: PlayerModel) {
    return aPlayer ? this.mFactions.find(F => F.FactionId == aPlayer.FactionId) : new FactionModel();
  }

  Origin(aPlayer: PlayerModel) {
    return aPlayer && aPlayer.Origin && this.mOrigins && this.mOrigins[aPlayer.Origin] ? this.mOrigins[aPlayer.Origin] : "";
  }

  PlayerColor(aPlayer: PlayerModel) {
    let FoundElevated = this.mPlayerService.ElevatedUser ? this.mPlayerService.ElevatedUser.find(U => U.steamId == aPlayer.SteamId) : null;
    if (FoundElevated) switch (FoundElevated.permission) {
      case 3: return "green"; //GameMaster
      case 6: return "brown"; //Moderator
      case 9: return "blue"; //Admin
    }

    let FoundBanned = this.mPlayerService.BannedUser ? this.mPlayerService.BannedUser.find(U => U.steamId == aPlayer.SteamId) : null;
    if (FoundBanned) return "red";

    return "black";
  }

  PlayerHint(aPlayer: PlayerModel) {
    let FoundElevated = this.mPlayerService.ElevatedUser ? this.mPlayerService.ElevatedUser.find(U => U.steamId == aPlayer.SteamId) : null;
    if (FoundElevated) switch (FoundElevated.permission) {
      case 3: return "GameMaster";
      case 6: return "Moderator";
      case 9: return "Admin"; 
    }

    let FoundBanned = this.mPlayerService.BannedUser ? this.mPlayerService.BannedUser.find(U => U.steamId == aPlayer.SteamId) : null;
    if (FoundBanned) return "Banned until " + FoundBanned.until.toLocaleString();

    return "";
  }

  GotoEntities(aPlayer: PlayerModel) {
    let foundFaction = this.mFactions.find(F => F.FactionId == aPlayer.FactionId);
    this.mStructureService.FilterPreset = aPlayer.PlayerName + (foundFaction ? " +" + foundFaction.Abbrev : "");
    this.router.navigate(['entities/structureslist'])
  }
}
