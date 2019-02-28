import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../services/role.service';
import { UserRole } from '../model/user';

@Component({
  selector: 'app-entities',
  templateUrl: './entities.component.html',
  styleUrls: ['./entities.component.less']
})
export class EntitiesComponent implements OnInit {
  UserRole = UserRole;

  constructor(
    public router: Router,
    public role: RoleService,
  ) {
  }

  ngOnInit() {
  }

}
