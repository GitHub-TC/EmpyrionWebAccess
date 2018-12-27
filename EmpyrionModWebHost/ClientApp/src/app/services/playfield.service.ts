import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { PlayfieldModel } from '../model/playfield-model';
import { AuthHubConnectionBuilder } from '../_helpers';
import { HubConnection } from '@aspnet/signalr';

@Injectable({
  providedIn: 'root'
})
export class PlayfieldService{
  hubConnection: HubConnection;
  private mPlayfieldsData: PlayfieldModel[];
  private error: any;

  private mPlayfields: BehaviorSubject<PlayfieldModel[]> = new BehaviorSubject(this.mPlayfieldsData);
  private readonly mPlayfieldsObservable: Observable<PlayfieldModel[]> = this.mPlayfields.asObservable();

  constructor(
    private http: HttpClient,
    private builder: AuthHubConnectionBuilder
  ) {
    this.hubConnection = builder.withAuthUrl('/hubs/playfields').build();

    // message coming from the server
    this.hubConnection.on("Update", this.ReadPlayfields);

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }

    this.ReadPlayfields();
  }

  ReadPlayfields() {
    let locationsSubscription = this.http.get<PlayfieldModel[]>("Playfield/Playfields")
      .pipe()
      .subscribe(
        L => this.mPlayfields.next(this.mPlayfieldsData = L),
        error => this.error = error // error path
      );

    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  get PlayfieldNames() {
    return this.mPlayfieldsObservable;
  }

}
