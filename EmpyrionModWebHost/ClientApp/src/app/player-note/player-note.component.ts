import { Component, OnInit, ViewChild } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { MatMenuTrigger, MatMenu } from '@angular/material/menu';
import { UserRole } from '../model/user';
import { RoleService } from '../services/role.service';
import { SystemInfoService } from '../services/systeminfo.service';

@Component({
  selector: 'app-player-note',
  templateUrl: './player-note.component.html',
  styleUrls: ['./player-note.component.less']
})
export class PlayerNoteComponent implements OnInit {
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;

  public Changed: boolean;
  Player: PlayerModel = {};
  UserRole = UserRole;

  constructor(
    private mPlayerService: PlayerService,
    public mSystemInfoService: SystemInfoService,
    public role: RoleService,
  ) {
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
    mPlayerService.GetCurrentPlayer().subscribe(P => this.SyncPlayer(P));
  }

  SyncPlayer(aPlayer: PlayerModel) {
    if (this.Changed || !aPlayer) return;

    this.Player = JSON.parse(JSON.stringify(aPlayer));
    this.Player.Food = Math.floor(this.Player.Food);
  }

  ngOnInit() {
  }

  SaveChanges() {
    this.contextMenuTrigger.closeMenu();
    this.mPlayerService.saveNote(this.Player);
    this.Changed = false;
  }

  DiscardChanges() {
    this.Changed = false;
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
  }


}
