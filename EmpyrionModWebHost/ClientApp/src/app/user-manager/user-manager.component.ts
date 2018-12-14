import { Component, OnInit, ViewChild } from '@angular/core';
import { UserService } from '../services/user.service';
import { User } from '../model/user';
import { Router } from '@angular/router';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

@Component({
  selector: 'app-user-manager',
  templateUrl: './user-manager.component.html',
  styleUrls: ['./user-manager.component.less']
})
export class UserManagerComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  users: User[] = [];
  newUser: User = { id: 0, username: "", password: "" };

  constructor(public mUserService: UserService, public router: Router) { }

  ngOnInit() {
    this.mUserService.getAll().subscribe(U => {
      this.users = U;
    });
  }

  deleteUser(aUser: User) {
    this.YesNo.openDialog({ title: "Delete user", question: aUser.username }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.mUserService.deleteUser(aUser);
      });
  }

  saveUser(aUser: User) {
    this.mUserService.saveUser(aUser);
  }

  createNewUser() {
    this.mUserService.createNewUser(this.newUser);
    this.newUser = { id: 0, username: "", password: "" };
  }
}
