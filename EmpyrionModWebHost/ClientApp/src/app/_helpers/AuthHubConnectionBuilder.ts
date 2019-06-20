import { Injectable } from '@angular/core';
import { HubConnectionBuilder } from '@aspnet/signalr';

import { AuthenticationService } from '../services/authentication.service';
import { User } from '../model/user';

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

  public build() {
    let hub = super.build();
    hub.onclose(() => setTimeout(() => hub.start(), 2000));
    return hub;
  }
}
