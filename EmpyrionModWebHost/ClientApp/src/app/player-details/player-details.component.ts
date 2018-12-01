import { Component, OnInit, Output, ViewChild } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { PlayfieldService } from '../services/playfield.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { MatMenu } from '@angular/material';

@Component({
  selector: 'app-player-details',
  templateUrl: './player-details.component.html',
  styleUrls: ['./player-details.component.less']
})
export class PlayerDetailsComponent implements OnInit {
  Player: PlayerModel;
  Playfields: string[];
  Factions: FactionModel[];
  @Output() Changed: boolean;
  @ViewChild(MatMenu) contextMenu: MatMenu;

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
    this.mPlayerService.saveUser(this.Player);
    this.Changed = false;
  }

  DiscardChanges() {
    this.Changed = false;
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
  }

}
