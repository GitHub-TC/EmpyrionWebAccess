import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatTabGroup } from '@angular/material';
import { RestoreFactoryItemsComponent } from '../restore-factoryitems/restore-factoryitems.component';

@Component({
  selector: 'app-restore',
  templateUrl: './restore.component.html',
  styleUrls: ['./restore.component.less']
})
export class RestoreComponent implements OnInit {
  @ViewChild(MatTabGroup, { static: true }) matTabGroup;
  focusComponent: ActivatedRoute;

  constructor(
    public router: Router,
    public activatedRoute: ActivatedRoute,
  ) {
    this.focusComponent = activatedRoute.firstChild;
  }

  ngOnInit() {
  }

  ngAfterViewInit() {
    if (typeof(this.focusComponent.component) == typeof(RestoreFactoryItemsComponent)) {
      this.matTabGroup.selectedIndex = 1;
    }
  }

}
