import { Component, OnInit } from '@angular/core';
import { UserService } from '../services/user.service';
import { User } from '../model/user';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user-manager',
  templateUrl: './user-manager.component.html',
  styleUrls: ['./user-manager.component.less']
})
export class UserManagerComponent implements OnInit {
  users: User[] = [];
  newUser: User = { id: 0, username: "", password: "" };

  constructor(private mUserService: UserService, public router: Router) { }

  ngOnInit() {
    this.mUserService.getAll().subscribe(U => {
      this.users = U;
    });
  }

  deleteUser(aUser: User) {
    this.mUserService.deleteUser(aUser);
  }

  saveUser(aUser: User) {
    this.mUserService.saveUser(aUser);
  }

  createNewUser() {
    this.mUserService.createNewUser(this.newUser);
    this.newUser = { id: 0, username: "", password: "" };
  }
}
