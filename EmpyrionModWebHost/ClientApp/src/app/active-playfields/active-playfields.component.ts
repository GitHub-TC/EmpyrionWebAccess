import { Component, OnInit, Input } from '@angular/core';

import { ActivePlayfieldModel } from '../model/activeplayfield-model';
import { PlayerModel } from '../model/player-model';

import { ACTIVEPLAYFIELDS } from '../model/activeplayfield-mock';
import { PositionService } from '../services/position.service';
import { ChatService } from '../services/chat.service';

@Component({
  selector: 'app-active-playfields',
  templateUrl: './active-playfields.component.html',
  styleUrls: ['./active-playfields.component.less']
})
export class ActivePlayfieldsComponent implements OnInit {
  playfields: ActivePlayfieldModel[] = ACTIVEPLAYFIELDS;
  mCurrentPlayer: PlayerModel;

  panels = [
    {
      title: 'panel 1',
      content: 'content 1',
      buttons: [
        {
          displayText: 'button1'
        },
        {
          displayText: 'button2'
        },
      ]
    },
    {
      title: 'panel 2',
      content: 'content 2',
      buttons: [
        {
          displayText: 'button2'
        },
        {
          displayText: 'button2'
        },
      ]
    },
  ];
  mPlayfieldsOpen: string[] = [];

  constructor(private mPositionService: PositionService, private mChatService: ChatService) { }

  ngOnInit() {
  }

  SavePosition(aPlayer: PlayerModel) {
    this.mPositionService.CurrentPosition = { playfield: aPlayer.playfield, pos: aPlayer.pos, rot: aPlayer.rot };
  }

  ChatTo(aPlayer: PlayerModel) {
    this.mChatService.ChatToPlayer(aPlayer);
  }

  get CurrentPlayer() {
    return this.mCurrentPlayer;
  }

  @Input() set CurrentPlayer(aPlayer: PlayerModel) {
    this.mCurrentPlayer = aPlayer;
  }

  IsClosed(aPlayfieldName: string) {
    return this.mPlayfieldsOpen.findIndex(P => P == aPlayfieldName) >= 0;
  }

  Open(aPlayfieldName: string) {
    let found = this.mPlayfieldsOpen.findIndex(P => P == aPlayfieldName);
    if (found >= 0) this.mPlayfieldsOpen = this.mPlayfieldsOpen.splice(found, 1);
  }

  Close(aPlayfieldName: string) {
    let found = this.mPlayfieldsOpen.findIndex(P => P == aPlayfieldName);
    if (found == -1) this.mPlayfieldsOpen.push(aPlayfieldName);
  }
}
