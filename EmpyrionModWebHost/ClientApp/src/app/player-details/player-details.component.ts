import { Component, OnInit, Output } from '@angular/core';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { PlayfieldService } from '../services/playfield.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';

@Component({
  selector: 'app-player-details',
  templateUrl: './player-details.component.html',
  styleUrls: ['./player-details.component.less']
})
export class PlayerDetailsComponent implements OnInit {
  Player: PlayerModel;
  Playfields: string[];
  Factions: FactionModel[];

  constructor(
    private mPlayfields: PlayfieldService,
    private mPlayerService: PlayerService,
    private mFactionService: FactionService
  ) {
    mPlayerService.GetCurrentPlayer().subscribe(P => this.Player = Object.assign({}, P));
    mFactionService.GetFactions().subscribe(F => this.Factions = F);
  }

  ngOnInit() {
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
  }

  SaveChanges() {
    this.mPlayerService.saveUser(this.Player);
  }

}
