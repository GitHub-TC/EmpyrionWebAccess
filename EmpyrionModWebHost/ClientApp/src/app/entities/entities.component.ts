import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';
import { StructuresListComponent } from '../structures-list/structures-list.component';
import { MatTabGroup } from '@angular/material';

@Component({
  selector: 'app-entities',
  templateUrl: './entities.component.html',
  styleUrls: ['./entities.component.less']
})
export class EntitiesComponent implements OnInit {
  @ViewChild(MatTabGroup) matTabGroup;
  focusComponent: ActivatedRoute;
  UserRole = UserRole;

  constructor(
    public router: Router,
    public activatedRoute: ActivatedRoute,
    public role: RoleService,
  ) {
    this.focusComponent = activatedRoute.firstChild;
  }

  ngOnInit() {
  }

  ngAfterViewInit() {
    if (typeof (this.focusComponent.component) == typeof (StructuresListComponent)) {
      this.matTabGroup.selectedIndex = 1;
    }
  }

}
