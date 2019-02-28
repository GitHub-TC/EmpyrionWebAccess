import { Component, OnInit, Input } from '@angular/core';

import { ActivePlayfieldModel } from '../model/activeplayfield-model';
import { PlayerModel } from '../model/player-model';

import { ACTIVEPLAYFIELDS } from '../model/activeplayfield-mock';
import { PositionService } from '../services/position.service';
import { ChatService } from '../services/chat.service';
import { PlayerService } from '../services/player.service';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { PlayfieldService } from '../services/playfield.service';
import { PlayfieldModel } from '../model/playfield-model';
import { Router } from '@angular/router';
import { UserRole } from '../model/user';
import { RoleService } from '../services/role.service';

@Component({
  selector: 'app-active-playfields',
  templateUrl: './active-playfields.component.html',
  styleUrls: ['./active-playfields.component.less']
})
export class ActivePlayfieldsComponent implements OnInit {
  playfields: ActivePlayfieldModel[] = ACTIVEPLAYFIELDS;
  mCurrentPlayer: PlayerModel;
  mFactions: FactionModel[];
  UserRole = UserRole;

  mPlayfieldsOpen: string[] = [];

  constructor(
    public  router: Router,
    private mFactionService: FactionService,
    private mPositionService: PositionService,
    private mChatService: ChatService,
    private mPlayerService: PlayerService,
    private mPlayfields: PlayfieldService,
    public role: RoleService,
  ) { }

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
    this.mFactionService.GetFactions().subscribe(F => this.mFactions = F);
  }

  SavePosition(aPlayer: PlayerModel) {
    this.mPositionService.CurrentPosition = {
      description: "Player: " + aPlayer.PlayerName,
      entityId : aPlayer.EntityId,
      playfield: aPlayer.Playfield,
      pos: { x: aPlayer.PosX, y: aPlayer.PosY, z: aPlayer.PosZ },
      rot: { x: aPlayer.RotX, y: aPlayer.RotY, z: aPlayer.RotZ }
    };
  }

  ChatTo(aPlayer: PlayerModel) {
    this.mChatService.ChatToPlayer(aPlayer);
  }

  Faction(aPlayer: PlayerModel) {
    return this.mFactions.find(F => F.FactionId == aPlayer.FactionId);
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
    setTimeout(() => {
      let found = this.mPlayfieldsOpen.findIndex(P => P == aPlayfieldName);
      if (found >= 0) this.mPlayfieldsOpen = this.mPlayfieldsOpen.splice(found, 1);
    }, 1000);
  }

  Close(aPlayfieldName: string) {
    setTimeout(() => {
      let found = this.mPlayfieldsOpen.findIndex(P => P == aPlayfieldName);
      if (found == -1) this.mPlayfieldsOpen.push(aPlayfieldName);
    }, 1000);
  }

  GotoPlayfield(aEvent, aPlayfield: PlayfieldModel) {
    aEvent.stopPropagation();
    this.mPlayfields.CurrentPlayfield = aPlayfield;
    this.router.navigate(['entities']);
  }
}
