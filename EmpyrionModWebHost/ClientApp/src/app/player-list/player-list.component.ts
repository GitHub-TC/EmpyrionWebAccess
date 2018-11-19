import { Component, OnInit, Input } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['online', 'playerName', 'origin', 'faction', 'playfield', 'pos', 'entityId', 'steamId'];
  players: PlayerModel[];

  message: string;
  autoscroll: boolean = true;

  constructor(private mPlayerService: PlayerService) {
  }

  ngOnInit() {
    this.mPlayerService.GetPlayers().subscribe(players => {
      this.players = players;
    });
  }

  get CurrentPlayerSteamId() {
    return this.mPlayerService.CurrentPlayer ? this.mPlayerService.CurrentPlayer.steamId : 0;
  }

  get CurrentPlayer() {
    return this.mPlayerService.CurrentPlayer;
  }

  @Input() set CurrentPlayer(aPlayer: PlayerModel) {
    this.mPlayerService.CurrentPlayer = aPlayer;
  }

}
