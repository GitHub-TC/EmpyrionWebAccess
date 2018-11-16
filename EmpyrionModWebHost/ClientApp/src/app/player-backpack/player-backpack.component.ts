import { Component, OnInit } from '@angular/core';
import { ItemStackModel } from '../model/itemstack-model';
import { ITEMS } from '../model/itemstack-mock';

@Component({
  selector: 'app-player-backpack',
  templateUrl: './player-backpack.component.html',
  styleUrls: ['./player-backpack.component.less']
})
export class PlayerBackpackComponent implements OnInit {
  Items: ItemStackModel[] = ITEMS;

  constructor() { }

  ngOnInit() {
  }

}
