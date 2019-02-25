import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable } from 'rxjs';

import { AuthenticationService } from '../services/authentication.service';
import { User } from '../model/user';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  currentUser: User;

  constructor(private authenticationService: AuthenticationService) {
    this.authenticationService.currentUser.subscribe(x => this.currentUser = x);
  }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.currentUser || !this.currentUser.token || request.url.startsWith("http://hubblesite.org")) return next.handle(request.clone());

    request = request.clone({
      setHeaders: {
        Authorization: `Bearer ${this.currentUser.token}`
      }
    });

    return next.handle(request);
  }
}
