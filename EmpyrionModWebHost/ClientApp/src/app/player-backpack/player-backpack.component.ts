import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { BackpackModel, EmptyBackpack, BackpackODataModel } from '../model/backpack-model';

import { BackpackService } from '../services/backpack.service';
import { MatMenu, MatMenuTrigger } from '@angular/material/menu';
import { PlayerService } from '../services/player.service';
import { Router } from '@angular/router';
import { ItemService } from '../services/item.service';
import { ItemStackModel } from '../model/itemstack-model';
import { SelectItemDialogComponent } from '../select-item-dialog/select-item-dialog.component';
import { PlayerModel } from '../model/player-model';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { UserRole } from '../model/user';
import { RoleService } from '../services/role.service';

@Component({
  selector: 'app-player-backpack',
  templateUrl: './player-backpack.component.html',
  styleUrls: ['./player-backpack.component.less']
})
export class PlayerBackpackComponent implements OnInit {
  @ViewChild(YesNoDialogComponent, { static: true }) YesNo: YesNoDialogComponent;
  @Input() backpack: BackpackModel = EmptyBackpack;
  @Input() WithEdit: boolean = true;
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;
  @ViewChild(SelectItemDialogComponent, { static: true }) selectItem: SelectItemDialogComponent;
  error: any;
  Player: PlayerModel;
  UserRole = UserRole;

  constructor(
    private http: HttpClient,
    public router: Router,
    private mBackpackService: BackpackService,
    private mPlayerService: PlayerService,
    private mItemService: ItemService,
    public role: RoleService,
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
    this.selectItem.openDialog(new ItemStackModel()).afterClosed().subscribe(
      (ItemStack: ItemStackModel) => {
        if (ItemStack.id == 0) return;

        this.http.post<any>('Backpacks/AddItem', {
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

  EditItem(aItem: ItemStackModel) {
    if (!this.WithEdit || !this.Player || !this.Player.Online) return;

    this.selectItem.openDialog(aItem).afterClosed().subscribe(
      (ItemStack: ItemStackModel) => {
        if (ItemStack.id == 0) return;

        if (ItemStack.count) {
          if (this.backpack.Bag    ) this.backpack.Bag     = this.backpack.Bag    .map(I => I.id == aItem.id && I.slotIdx == aItem.slotIdx ? ItemStack : I);
          if (this.backpack.Toolbar) this.backpack.Toolbar = this.backpack.Toolbar.map(I => I.id == aItem.id && I.slotIdx == aItem.slotIdx ? ItemStack : I);
        }
        else {
          if (this.backpack.Bag    ) this.backpack.Bag     = this.backpack.Bag    .filter(I => I.id != aItem.id || I.slotIdx != aItem.slotIdx);
          if (this.backpack.Toolbar) this.backpack.Toolbar = this.backpack.Toolbar.filter(I => I.id != aItem.id || I.slotIdx != aItem.slotIdx);
        }

        this.http.post<BackpackODataModel>("Backpacks/SetBackpack", this.backpack)
          .subscribe(
            error => this.error = error // error path
          );
      }
    );
  }
}
