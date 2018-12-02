import { Component, OnInit, Input, ViewChild } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';
import { ChatService } from '../services/chat.service';
import { PositionService } from '../services/position.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatTableDataSource, MatSort } from '@angular/material';
import { PLAYER } from '../model/player-mock';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['Online', 'PlayerName', 'Origin', 'Faction', 'Playfield', 'PosX', 'PosY', 'PosZ', 'EntityId', 'SteamId'];
  players: MatTableDataSource<PlayerModel> = new MatTableDataSource([]);
  displayFilter: boolean;

  @ViewChild(MatSort) sort: MatSort;

  message: string;
  autoscroll: boolean = true;
  mFactions: FactionModel[];

  constructor(
    private mFactionService: FactionService,
    private mPlayerService: PlayerService,
    private mPositionService: PositionService,
    private mChatService: ChatService) {
  }

  ngOnInit() {
    this.mPlayerService.GetPlayers().subscribe(players => {
      this.players.data = players;
    });
    this.mFactionService.GetFactions().subscribe(F => this.mFactions = F);
  }

  ngAfterViewInit() {
    this.players.sort = this.sort;
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
}
