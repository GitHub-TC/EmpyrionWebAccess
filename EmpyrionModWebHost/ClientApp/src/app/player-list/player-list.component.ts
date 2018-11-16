import { Component, OnInit } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['online', 'playerName', 'origin', 'faction', 'playfield', 'posX', 'posY', 'posZ', 'entityId', 'steamId'];
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

}
