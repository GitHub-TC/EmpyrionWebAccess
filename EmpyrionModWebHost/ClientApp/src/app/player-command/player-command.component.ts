import { Component, OnInit, ViewChild } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { MatMenuTrigger, MatMenu } from '@angular/material/menu';
import { UserRole } from '../model/user';
import { RoleService } from '../services/role.service';
import { SystemInfoService } from '../services/systeminfo.service';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-player-command',
  templateUrl: './player-command.component.html',
  styleUrls: ['./player-command.component.less']
})
export class PlayerCommandComponent implements OnInit {
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;

  PlayerCommandControl = new FormControl();
  Player: PlayerModel = {};
  UserRole = UserRole;
  CommandHistory: string = "";

  constructor(
    private mPlayerService: PlayerService,
    public mSystemInfoService: SystemInfoService,
    public role: RoleService,
  ) {
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
    mPlayerService.GetCurrentPlayer().subscribe(P => this.SyncPlayer(P));
  }

  SyncPlayer(aPlayer: PlayerModel) {
    if (!aPlayer) return;

    this.Player = JSON.parse(JSON.stringify(aPlayer));
    this.Player.Food = Math.floor(this.Player.Food);
  }

  ngOnInit() {
  }

  Execute() {
    this.contextMenuTrigger.closeMenu();
    if (this.PlayerCommandControl.value) {
      this.mPlayerService.executeCommand(this.Player, this.PlayerCommandControl.value);
      this.CommandHistory += new Date().toLocaleString() + ": " + this.PlayerCommandControl.value + "\n";
      this.PlayerCommandControl.setValue("");
    }
  }


}
