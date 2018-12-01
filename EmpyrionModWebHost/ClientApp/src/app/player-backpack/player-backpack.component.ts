import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { BackpackModel, EmptyBackpack } from '../model/backpack-model';

import { BackpackService } from '../services/backpack.service';
import { MatMenu } from '@angular/material';
import { PlayerService } from '../services/player.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-player-backpack',
  templateUrl: './player-backpack.component.html',
  styleUrls: ['./player-backpack.component.less']
})
export class PlayerBackpackComponent implements OnInit {
  backpack: BackpackModel = EmptyBackpack;
  @ViewChild(MatMenu) contextMenu: MatMenu;
  error: any;

  constructor(
    private http: HttpClient,
    public router: Router,
    private mBackpackService: BackpackService,
    private mPlayerService: PlayerService,
  ) { }

  ngOnInit() {
  }

  @Input() set PlayerSteamId(aPlayerSteamId: string) {
    console.log(aPlayerSteamId);
    this.backpack         = EmptyBackpack;
    this.backpack.SteamId = aPlayerSteamId;
    this.mBackpackService.GetBackpack(aPlayerSteamId).subscribe(B => this.backpack = B);
  }

  AddItem() {
    this.http.post<any>('BackpackApi/AddItem', {
      Id: this.mPlayerService.CurrentPlayer.EntityId,
      itemStack:
      {
        id: 42,
        count: 11,
        slotIdx: 1,
        ammo: 0,
        decay: 2,
      }
    }).pipe()
      .subscribe(
        () => { },
        error => this.error = error // error path
      );

  }
}
