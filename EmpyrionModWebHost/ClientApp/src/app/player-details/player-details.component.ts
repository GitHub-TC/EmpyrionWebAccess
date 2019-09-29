import { Component, OnInit, Output, ViewChild } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { PlayfieldService } from '../services/playfield.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatMenu, MatMenuTrigger } from '@angular/material';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { PlayfieldModel } from '../model/playfield-model';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';
import { BehaviorSubject, Observable } from 'rxjs';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-player-details',
  templateUrl: './player-details.component.html',
  styleUrls: ['./player-details.component.less']
})
export class PlayerDetailsComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  SelectedFaction = new FormControl();
  Player: PlayerModel = {};
  Playfields: PlayfieldModel[] = [];
  Factions: FactionModel[] = [];
  public Changed: boolean;
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;
  Roles = [{ id: 0, text: 'Owner' }, { id: 1, text: 'Admin' }, { id: 2, text: 'Member' }];
  UserRole = UserRole;

  private filteredFactions: BehaviorSubject<FactionModel[]> = new BehaviorSubject(this.Factions);
  public readonly filteredFactionsObservable: Observable<FactionModel[]> = this.filteredFactions.asObservable();

  constructor(
    private mPlayfields: PlayfieldService,
    private mPlayerService: PlayerService,
    private mFactionService: FactionService,
    public role: RoleService,
  ) {
  }

  SyncPlayer(aPlayer: PlayerModel) {
    if (this.Changed || !aPlayer) return;

    this.Player = JSON.parse(JSON.stringify(aPlayer));
    this.Player.Food = Math.floor(this.Player.Food);
  }

  ngOnInit() {
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
    this.mPlayerService.GetCurrentPlayer().subscribe(P => this.SyncPlayer(P));
    this.mFactionService.GetFactions().subscribe(F => {
      this.Factions = F;
      let found = this.Factions.filter(option => option.FactionId == this.Player.FactionId);
      if (found.length == 1) this.SelectedFaction.setValue(found[0].Abbrev);
    });

    this.SelectedFaction.valueChanges.subscribe(F => {
      let found = this.Factions.filter(option => option.Abbrev.toLowerCase().startsWith(F.toLowerCase()));
      if (found.length == 1) {
        this.Player.FactionId = found[0].FactionId;
        this.Changed = true;
      }
      this.filteredFactions.next(found);
    });
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

  Kick(aPlayer: PlayerModel) {
    this.contextMenuTrigger.closeMenu();
    this.YesNo.openDialog({ title: "Kick player", question: aPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mPlayerService.KickPlayer(this.Player);
      });
  }

  Ban(aPlayer: PlayerModel, aDuration: string) {
    this.contextMenuTrigger.closeMenu();
    this.YesNo.openDialog({ title: "Ban player for " + aDuration, question: aPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mPlayerService.BanPlayer(this.Player, aDuration);
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

  SetRole(aPlayer: PlayerModel, aRole: string) {
    this.contextMenuTrigger.closeMenu();
    this.YesNo.openDialog({ title: "Set role for player to " + aRole, question: aPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mPlayerService.SetRoleOfPlayer(this.Player, aRole);
      });
  }


  PlayerHint(aPlayer: PlayerModel) {
    let FoundElevated = this.mPlayerService.ElevatedUser ? this.mPlayerService.ElevatedUser.find(U => U.steamId == aPlayer.SteamId) : null;
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
