import { Component, OnInit, ViewChild } from '@angular/core';
import { MatMenu } from '@angular/material';

@Component({
  selector: 'app-factory',
  templateUrl: './factory.component.html',
  styleUrls: ['./factory.component.less']
})
export class FactoryComponent implements OnInit {
  @ViewChild(MatMenu) contextMenu: MatMenu;

  constructor() { }

  ngOnInit() {
  }

}
