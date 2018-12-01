import { Component, OnInit, Input, Inject, ViewChild } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA, MatSelect } from '@angular/material';
import { HttpClient } from '@angular/common/http';
import { ItemStackModel, ItemInfoModel } from '../model/itemstack-model';
import { ItemService } from '../services/item.service';
import { FormControl } from '@angular/forms';
import { ReplaySubject, Subject } from 'rxjs';
import { MatSelectSearchComponent } from '../mat-select-search/mat-select-search.component';
import { takeUntil, take } from 'rxjs/operators';

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
  SelectedItemInfo: number;
  error: any;

  @ViewChild('singleSelect') singleSelect: MatSelect;

  public filterCtrl: FormControl = new FormControl();
  public filteredItems: ReplaySubject<ItemInfoModel[]> = new ReplaySubject<ItemInfoModel[]>(1);

  private _onDestroy = new Subject<void>();

  constructor(private http: HttpClient,
    public dialogRef: MatDialogRef<SelectItemDialogContentComponent>,
    public ItemService: ItemService,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.ItemStack = Object.assign({}, data.ItemStack);
  }

  ngOnInit() {
    this.filteredItems.next(this.ItemService.ItemInfo.slice());

    // listen for search field value changes
    this.filterCtrl.valueChanges
      .pipe(takeUntil(this._onDestroy))
      .subscribe(() => {
        this.filterBanks();
      });
  }

  ngAfterViewInit() {
    this.setInitialValue();
  }

  ngOnDestroy() {
    this._onDestroy.next();
    this._onDestroy.complete();
  }

  /**
   * Sets the initial value after the filteredBanks are loaded initially
   */
  private setInitialValue() {
    this.filteredItems
      .pipe(take(1), takeUntil(this._onDestroy))
      .subscribe(() => {
        // setting the compareWith property to a comparison function 
        // triggers initializing the selection according to the initial value of 
        // the form control (i.e. _initializeSelection())
        // this needs to be done after the filteredBanks are loaded initially 
        // and after the mat-option elements are available
        this.singleSelect.compareWith = (a: ItemInfoModel, b: ItemInfoModel) => a.id === b.id;
      });
  }

  private filterBanks() {
    // get the search keyword
    let search = this.filterCtrl.value;
    if (!search) {
      this.filteredItems.next(this.ItemService.ItemInfo.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filteredItems.next(
      this.ItemService.ItemInfo.filter(item => (item.id + " " + item.name).toLowerCase().indexOf(search) > -1)
    );
  }

  AddItem() {
    this.ItemStack.id = this.SelectedItemInfo;
    this.dialogRef.close(this.ItemStack);
  }

}
