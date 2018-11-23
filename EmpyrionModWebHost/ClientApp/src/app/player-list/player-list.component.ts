import { Component, OnInit, Input } from '@angular/core';

import { PlayerService } from '../services/player.service';

import { PlayerModel } from '../model/player-model';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-player-list',
  templateUrl: './player-list.component.html',
  styleUrls: ['./player-list.component.less']
})
export class PlayerListComponent implements OnInit {
  displayedColumns = ['Online', 'PlayerName', 'Origin', 'Faction', 'Playfield', 'Pos', 'EntityId', 'SteamId'];
  players: PlayerModel[];

  message: string;
  autoscroll: boolean = true;

  constructor(private mPlayerService: PlayerService, private mChatService: ChatService) {
  }

  ngOnInit() {
    this.mPlayerService.GetPlayers().subscribe(players => {
      this.players = players;
    });
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

  ChatTo(aPlayer: PlayerModel) {
    this.mChatService.ChatToPlayer(aPlayer);
  }
}
