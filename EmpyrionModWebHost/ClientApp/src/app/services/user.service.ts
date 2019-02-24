import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { User } from '../model/user';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private mUsers: User[];

  private users: BehaviorSubject<User[]> = new BehaviorSubject(this.mUsers);
  public readonly playersObservable: Observable<User[]> = this.users.asObservable();
  error: any;

  constructor(private http: HttpClient) { }

  getAll(): Observable<User[]> {
    this.http.get<User[]>("users")
      .pipe()
      .subscribe(
        U => this.users.next(this.mUsers = U),
        error => this.error = error // error path
      );

    return this.playersObservable;
  }

  createNewUser(newUser: User) {
    this.http.post<any>('users/register', {
      Username: newUser.username,
      Password: newUser.password,
      InGameSteamId: newUser.inGameSteamId,
      Role: newUser.role,
    })
      .pipe()
      .subscribe(
        () => this.getAll(),
        error => this.error = error // error path
      );
  }

  saveUser(aUser: User): any {
    this.http.post<any>('users/update', {
      Id: aUser.id,
      Username: aUser.username,
      Password: aUser.password,
      InGameSteamId: aUser.inGameSteamId,
      Role: aUser.role,
    })
      .pipe()
      .subscribe(
        () => this.getAll(),
        error => this.error = error // error path
      );
  }

  deleteUser(aUser: User): any {
    this.http.delete("users/" + aUser.id)
      .pipe()
      .subscribe(() => this.getAll());
  }

}
