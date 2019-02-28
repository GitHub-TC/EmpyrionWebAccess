import { Component, OnInit, Input, ViewChild } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';
import { ChatService } from '../services/chat.service';
import { PositionService } from '../services/position.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatTableDataSource, MatSort } from '@angular/material';
import { PLAYER } from '../model/player-mock';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['Online', 'PlayerName', 'Origin', 'Faction', 'Playfield', 'PosX', 'PosY', 'PosZ', 'LastOnline', 'OnlineHours', 'EntityId', 'SteamId'];
  players: MatTableDataSource<PlayerModel> = new MatTableDataSource([]);
  displayFilter: boolean;

  @ViewChild(MatSort) sort: MatSort;

  message: string;
  autoscroll: boolean = true;
  mFactions: FactionModel[];
  mSelectedPlayfield: string;
  mAllPlayers: PlayerModel[];
  UserRole = UserRole;

  constructor(
    private mFactionService: FactionService,
    private mPlayerService: PlayerService,
    private mPositionService: PositionService,
    private mChatService: ChatService,
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
  }

  ngAfterViewInit() {
    this.players.sort = this.sort;
    this.players.sortingDataAccessor = (D, H) => typeof(D[H])==="string" ? ("" + D[H]).toLowerCase() : D[H];
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

  ChatTo(aPlayer: PlayerModel) {
    this.mChatService.ChatToPlayer(aPlayer);
  }

  Faction(aPlayer: PlayerModel) {
    return aPlayer ? this.mFactions.find(F => F.FactionId == aPlayer.FactionId) : "";
  }

  PlayerColor(aPlayer: PlayerModel) {
    let FoundElevated = this.mPlayerService.ElevatedUser.find(U => U.steamId == aPlayer.SteamId);
    if (FoundElevated) switch (FoundElevated.permission) {
      case 3: return "green"; //GameMaster
      case 6: return "brown"; //Moderator
      case 9: return "blue"; //Admin
    }

    let FoundBanned = this.mPlayerService.BannedUser.find(U => U.steamId == aPlayer.SteamId);
    if (FoundBanned) return "red";

    return "black";
  }

  PlayerHint(aPlayer: PlayerModel) {
    let FoundElevated = this.mPlayerService.ElevatedUser.find(U => U.steamId == aPlayer.SteamId);
    if (FoundElevated) switch (FoundElevated.permission) {
      case 3: return "GameMaster";
      case 6: return "Moderator";
      case 9: return "Admin"; 
    }

    let FoundBanned = this.mPlayerService.BannedUser.find(U => U.steamId == aPlayer.SteamId);
    if (FoundBanned) return "Banned until " + FoundBanned.until.toLocaleString();

    return "";
  }

}
