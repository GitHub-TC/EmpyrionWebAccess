import { Injectable } from '@angular/core';
import { AuthenticationService } from './authentication.service';
import { User, UserRole } from '../model/user';
import { Enum } from '../model/Enum';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  CurrentUser: User;
  CurrentRole: UserRole
  authService: AuthenticationService;
  Roles: Enum<UserRole>[];


  constructor(private authenticationService: AuthenticationService) {
    this.Roles = Object.keys(UserRole).map(key => { return <Enum<UserRole>><any>{ key: key, value: UserRole[key] }; });

    this.authService = authenticationService;
    this.CurrentUser = this.authService.currentUserValue;
    this.CurrentRole = this.CurrentUser ? <UserRole><any>(this.Roles.find(R => R.key == this.CurrentUser.role).value) : UserRole.None;
    this.authService.currentUser.subscribe(U => {
      this.CurrentUser = U;
      this.CurrentRole = this.CurrentUser ? <UserRole><any>(this.Roles.find(R => R.key == this.CurrentUser.role).value) : UserRole.None;
    });
  }

  public is(aRole: UserRole) {
    return this.CurrentUser && this.CurrentRole <= aRole;
  }
}
