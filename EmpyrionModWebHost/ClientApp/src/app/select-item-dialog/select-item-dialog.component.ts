import { Component, OnInit, Input, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { HttpClient } from '@angular/common/http';
import { ItemStackModel, ItemInfoModel } from '../model/itemstack-model';
import { ItemService } from '../services/item.service';
import { FormControl } from '@angular/forms';
import { Observable } from 'rxjs';
import { startWith, map } from 'rxjs/operators';

@Component({
  selector: 'app-select-item-dialog',
  templateUrl: './select-item-dialog.component.html',
  styleUrls: ['./select-item-dialog.component.less']
})
export class SelectItemDialogComponent implements OnInit {
  @Input() ItemStack: ItemStackModel;

  constructor(public dialog: MatDialog) { }

  ngOnInit(): void {
  }

  public openDialog() {
    const dialogRef = this.dialog.open(SelectItemDialogContentComponent, {
      data: { ItemStack: this.ItemStack }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log(`Dialog result: ${result}`);
    });

    return dialogRef;
  }
}

@Component({
  selector: 'app-select-item-dialog-content',
  templateUrl: 'select-item-dialog-content.component.html',
  styleUrls: ['./select-item-dialog.component.less']
})
export class SelectItemDialogContentComponent implements OnInit {
  @Input() ItemStack: ItemStackModel = { id: 0, count: 0, ammo: 0, slotIdx: 0, decay: 0};
  SelectedItem = new FormControl();
  SelectedItemInfo: ItemInfoModel = { id: 0, name: "" };
  filteredOptions: Observable<ItemInfoModel[]>;
  error: any;

  constructor(
    public dialogRef: MatDialogRef<SelectItemDialogContentComponent>,
    public ItemService: ItemService,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.ItemStack = Object.assign({}, data.ItemStack);
  }

  ngOnInit() {
    this.filteredOptions = this.SelectedItem.valueChanges
      .pipe(
        startWith(''),
        map(value => {
          let found = this.ItemService.ItemInfo.filter(option => option.name === value);
          if (found.length == 1) this.SelectedItemInfo = found[0];
          return this._filter(value);
      })
    );
  }

  private _filter(value: string): ItemInfoModel[] {
    const filterValue = value.toLowerCase();

    return this.ItemService.ItemInfo.filter(option => (option.id + option.name.toLowerCase()).includes(filterValue));
  }

  AddItem() {
    this.ItemStack.id = this.SelectedItemInfo.id;
    this.dialogRef.close(this.ItemStack);
  }

}
