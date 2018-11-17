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

  @Input() set EntityPlayerId(aEntityPlayerId: number) {
    console.log(aEntityPlayerId);
    this.backpack = this.mBackpackService.GetBackpack(aEntityPlayerId);
    if (!this.backpack) this.backpack = EmptyBackpack;
  }

}
