import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { BackpackModel, EmptyBackpack } from '../model/backpack-model';

import { BackpackService } from '../services/backpack.service';
import { MatMenu, MatMenuTrigger } from '@angular/material';
import { PlayerService } from '../services/player.service';
import { Router } from '@angular/router';
import { ItemService } from '../services/item.service';
import { ItemStackModel } from '../model/itemstack-model';
import { SelectItemDialogComponent } from '../select-item-dialog/select-item-dialog.component';
import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-player-backpack',
  templateUrl: './player-backpack.component.html',
  styleUrls: ['./player-backpack.component.less']
})
export class PlayerBackpackComponent implements OnInit {
  backpack: BackpackModel = EmptyBackpack;
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;
  @ViewChild(SelectItemDialogComponent) selectNewItem: SelectItemDialogComponent;
  AddItemStack: ItemStackModel;
  error: any;
  Player: PlayerModel;

  constructor(
    private http: HttpClient,
    public router: Router,
    private mBackpackService: BackpackService,
    private mPlayerService: PlayerService,
    private mItemService: ItemService,
  ) { }

  ngOnInit() {
    this.mPlayerService.GetCurrentPlayer().subscribe(P => this.Player = P);
  }

  @Input() set PlayerSteamId(aPlayerSteamId: string) {
    console.log(aPlayerSteamId);
    this.backpack         = EmptyBackpack;
    this.backpack.SteamId = aPlayerSteamId;
    this.mBackpackService.GetBackpack(aPlayerSteamId).subscribe(B => this.backpack = B);
  }

  GetName(mStack: ItemStackModel) {
    if (!this.mItemService || !this.mItemService.ItemInfo) return mStack.id;

    let found = this.mItemService.ItemInfo.find(I => I.id == mStack.id);
    return found ? found.name : mStack.id;
  }

  AddItem() {
    this.contextMenuTrigger.closeMenu();
    this.selectNewItem.openDialog().afterClosed().subscribe(
      (ItemStack: ItemStackModel) => {
        if (ItemStack.id == 0) return;

        this.http.post<any>('BackpackApi/AddItem', {
          Id: this.mPlayerService.CurrentPlayer.EntityId,
          itemStack: ItemStack,
        }).pipe()
          .subscribe(
            () => { },
            error => this.error = error // error path
          );
      }
    );
  }
}
