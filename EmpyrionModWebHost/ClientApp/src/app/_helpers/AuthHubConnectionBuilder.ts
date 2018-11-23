import { Injectable } from '@angular/core';
import { HubConnectionBuilder } from '@aspnet/signalr';

import { AuthenticationService } from '../_services';
import { User } from '../_models';

@Injectable({
  providedIn: 'root'
})
export class AuthHubConnectionBuilder extends HubConnectionBuilder {
  currentUser: User;

  constructor(private authenticationService: AuthenticationService){ super();
    this.authenticationService.currentUser.subscribe(x => this.currentUser = x);
  };

  withAuthUrl(url: string): HubConnectionBuilder {
    return super.withUrl(url, { accessTokenFactory: () => this.currentUser.token });
  }
}
