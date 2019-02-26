import { Injectable } from '@angular/core';
import { AuthenticationService } from './authentication.service';
import { User, UserRole } from '../model/user';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  CurrentUser: User;
  authService: AuthenticationService;

  constructor(private authenticationService: AuthenticationService) {
    this.authService = authenticationService;
    this.CurrentUser = this.authService.currentUserValue;
    this.authService.currentUser.subscribe(U => this.CurrentUser);
  }

  public is(aRole: UserRole) {
    return this.CurrentUser && this.CurrentUser.role <= aRole;
  }
}
