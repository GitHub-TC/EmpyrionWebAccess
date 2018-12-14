import { Component, OnInit, Output, ViewChild } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { PlayfieldService } from '../services/playfield.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatMenu, MatMenuTrigger } from '@angular/material';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

@Component({
  selector: 'app-player-details',
  templateUrl: './player-details.component.html',
  styleUrls: ['./player-details.component.less']
})
export class PlayerDetailsComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  Player: PlayerModel;
  Playfields: string[];
  Factions: FactionModel[];
  @Output() Changed: boolean;
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;

  constructor(
    private mPlayfields: PlayfieldService,
    private mPlayerService: PlayerService,
    private mFactionService: FactionService
  ) {
    mPlayerService.GetCurrentPlayer().subscribe(P => this.SyncPlayer(P));
    mFactionService.GetFactions().subscribe(F => this.Factions = F);
  }

  SyncPlayer(aPlayer: PlayerModel) {
    if (this.Changed) return;

    this.Player = Object.assign({}, aPlayer);
    this.Player.Food = Math.floor(this.Player.Food);
  }

  ngOnInit() {
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
  }

  SaveChanges() {
    this.contextMenuTrigger.closeMenu();
    if (this.Player.FactionId) this.Player.FactionGroup = 0;  // gehört einer Faction an
    this.mPlayerService.saveUser(this.Player);
    this.Changed = false;
  }

  DiscardChanges() {
    this.Changed = false;
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
  }

  Ban(aPlayer: PlayerModel) {
    this.contextMenuTrigger.closeMenu();
    this.YesNo.openDialog({ title: "Ban player", question: aPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mPlayerService.BanPlayer(this.Player);
      });
  }

  UnBan(aPlayer: PlayerModel) {
    this.contextMenuTrigger.closeMenu();
    this.mPlayerService.UnBanPlayer(this.Player);
  }

  Wipe(aPlayer: PlayerModel) {
    this.contextMenuTrigger.closeMenu();
    this.YesNo.openDialog({ title: "Wipe player", question: aPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mPlayerService.WipePlayer(this.Player);
      });
  }
}
