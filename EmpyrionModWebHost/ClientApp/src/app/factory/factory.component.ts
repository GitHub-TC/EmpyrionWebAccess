import { Component, OnInit, ViewChild, Output } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatMenu, MatMenuTrigger } from '@angular/material';
import { PlayerModel } from '../model/player-model';
import { PlayerService } from '../services/player.service';
import { ItemStackModel } from '../model/itemstack-model';
import { ItemService } from '../services/item.service';
import { SelectItemDialogComponent } from '../select-item-dialog/select-item-dialog.component';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';

interface BlueprintResources {
  playerId: number;
  itemStacks: ItemStackModel[];
  replaceExisting?: boolean;
}

@Component({
  selector: 'app-factory',
  templateUrl: './factory.component.html',
  styleUrls: ['./factory.component.less']
})
export class FactoryComponent implements OnInit {
  @ViewChild(MatMenu) contextMenu: MatMenu;
  @ViewChild(MatMenuTrigger) contextMenuTrigger: MatMenuTrigger;
  @ViewChild(SelectItemDialogComponent) selectNewItem: SelectItemDialogComponent;
  Player: PlayerModel;
  Resources: BlueprintResources;
  public Changed: boolean = false;
  error: any;
  UserRole = UserRole;

  constructor(
    private http: HttpClient,
    private mPlayerService: PlayerService,
    private mItemService: ItemService,
    public role: RoleService,
  ) { }

  ngOnInit() {
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
    this.mPlayerService.GetCurrentPlayer().subscribe(P => this.SyncPlayer(this.Player = P));
  }

  SyncPlayer(aPlayer: PlayerModel) {
    if (this.Changed || !aPlayer) return;

    this.http.get<BlueprintResources>('Factory/GetBlueprintResources/' + aPlayer.EntityId)
      .pipe()
      .subscribe(
        B => this.Resources = B,
        error => this.error = error // error path
      );
  }

  DiscardChanges() {
    this.Changed = false;
    this.SyncPlayer(this.mPlayerService.CurrentPlayer);
  }

  SetBlueprintResources() {
    this.contextMenuTrigger.closeMenu();
    this.http.post('Factory/SetBlueprintResources', this.Resources)
      .pipe()
      .subscribe(
        B => { },
        error => this.error = error // error path
    );

    this.Changed = false;
  }

  FinishBlueprint() {
    this.contextMenuTrigger.closeMenu();
    this.http.get<BlueprintResources>('Factory/FinishBlueprint/' + this.Resources.playerId)
      .pipe()
      .subscribe(
        B => { },
        error => this.error = error // error path
      );
  }

  GetName(mStack: ItemStackModel) {
    if (!this.mItemService || !this.mItemService.ItemInfo) return mStack.id;

    let found = this.mItemService.ItemInfo.find(I => I.id == mStack.id);
    return found ? found.name : mStack.id;
  }

  AddItem() {
    this.contextMenuTrigger.closeMenu();
    this.selectNewItem.openDialog(new ItemStackModel()).afterClosed().subscribe(
      (ItemStack: ItemStackModel) => {
        if (ItemStack.id == 0) return;

        this.Changed = true;
        this.Resources.itemStacks.push(ItemStack);
      }
    );
  }

}
