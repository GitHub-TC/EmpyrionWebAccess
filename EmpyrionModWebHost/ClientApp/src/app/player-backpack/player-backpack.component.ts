import { Component, OnInit, Input } from '@angular/core';

import { BackpackModel, EmptyBackpack } from '../model/backpack-model';

import { BackpackService } from '../services/backpack.service';

@Component({
  selector: 'app-player-backpack',
  templateUrl: './player-backpack.component.html',
  styleUrls: ['./player-backpack.component.less']
})
export class PlayerBackpackComponent implements OnInit {
  backpack: BackpackModel = EmptyBackpack;

  constructor(private mBackpackService: BackpackService) { }

  ngOnInit() {
  }

  @Input() set PlayerSteamId(aPlayerSteamId: string) {
    console.log(aPlayerSteamId);
    this.backpack         = EmptyBackpack;
    this.backpack.SteamId = aPlayerSteamId;
    this.mBackpackService.GetBackpack(aPlayerSteamId).subscribe(B => this.backpack = B);
  }

}
