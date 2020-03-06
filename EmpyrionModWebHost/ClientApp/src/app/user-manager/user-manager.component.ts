import { Component, OnInit, ViewChild } from '@angular/core';
import { UserService } from '../services/user.service';
import { User, UserRole } from '../model/user';
import { Router } from '@angular/router';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { Enum } from '../model/Enum';

export enum RoleEnum {
  ServerAdmin = "Server Admin",
  InGameAdmin = "In Game Admin",
  Moderator   = "Moderator",
  GameMaster  = "Game Master",
  VIP         = "VIP Player",
  Player      = "Player",
  None        = "No Access"
}

@Component({
  selector: 'app-user-manager',
  templateUrl: './user-manager.component.html',
  styleUrls: ['./user-manager.component.less']
})
export class UserManagerComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  Roles: Enum<RoleEnum>[];
  users: User[] = [];
  newUser: User = { id: 0, username: "", password: "", role: UserRole.None };

  constructor(public mUserService: UserService, public router: Router) {
    this.Roles = Object.keys(RoleEnum).map(key => { return <Enum<RoleEnum>>{ key: key, value: RoleEnum[key] }; });
  }

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

  createNewUser(aUser: User) {
    this.mUserService.createNewUser(aUser);
    this.newUser = { id: 0, username: "", password: "", role: UserRole.None };
  }
}
