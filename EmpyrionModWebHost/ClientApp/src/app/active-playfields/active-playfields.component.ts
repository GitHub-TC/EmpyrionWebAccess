import { Component, OnInit, Input } from '@angular/core';

import { ActivePlayfieldModel } from '../model/activeplayfield-model';
import { PlayerModel } from '../model/player-model';

import { ACTIVEPLAYFIELDS } from '../model/activeplayfield-mock';
import { PositionService } from '../services/position.service';
import { ChatService } from '../services/chat.service';
import { PlayerService } from '../services/player.service';

@Component({
  selector: 'app-active-playfields',
  templateUrl: './active-playfields.component.html',
  styleUrls: ['./active-playfields.component.less']
})
export class ActivePlayfieldsComponent implements OnInit {
  playfields: ActivePlayfieldModel[] = ACTIVEPLAYFIELDS;
  mCurrentPlayer: PlayerModel;

  mPlayfieldsOpen: string[] = [];

  constructor(private mPositionService: PositionService, private mChatService: ChatService, private mPlayerService: PlayerService) { }

  ngOnInit() {
    this.mPlayerService.GetPlayers().subscribe(players => {
      let PF: ActivePlayfieldModel[] = [];
      players.filter(P => P.Online).map(P => {
        let FoundPF = PF.find(F => F.name == P.Playfield);
        if (!FoundPF) PF.push(FoundPF = { name: P.Playfield, players: [] });
        FoundPF.players.push(P);
      });
      this.playfields = PF.sort((A, B) => A.name.localeCompare(B.name));
    });
  }

  SavePosition(aPlayer: PlayerModel) {
    this.mPositionService.CurrentPosition = {
      playfield: aPlayer.Playfield, pos: { x: aPlayer.PosX, y: aPlayer.PosY, z: aPlayer.PosZ }, rot: { x: aPlayer.RotX, y: aPlayer.RotY, z: aPlayer.RotZ } };
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
