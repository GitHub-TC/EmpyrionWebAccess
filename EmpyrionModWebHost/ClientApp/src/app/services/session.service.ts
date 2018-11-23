import { Injectable, Input } from '@angular/core';
import { SessionModel } from '../model/session-model';
import { SESSION } from '../model/session-mock';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private session: SessionModel = SESSION;

  constructor() { }

  get CurrentSession() {
    return this.session;
  }

  @Input() set CurrentSession(aSession: SessionModel) {
    this.session = aSession;
  }

}
