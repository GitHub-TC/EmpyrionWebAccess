import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ItemInfoModel } from '../model/itemstack-model';

@Injectable({
  providedIn: 'root'
})
export class ItemService {
  error: any;
  public ItemInfo: ItemInfoModel[];

  constructor(private http: HttpClient) {
    this.http.get<ItemInfoModel[]>("gameplay/GetAllItems")
      .subscribe(
        I => this.ItemInfo = I,
        error => this.error = error // error path
      );
  }
}

