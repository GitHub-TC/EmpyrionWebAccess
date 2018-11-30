import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PlayfieldService{
  private mPlayfields: string[];
  private error: any;

  private playfields: BehaviorSubject<string[]> = new BehaviorSubject(this.mPlayfields);
  private readonly playfieldsObservable: Observable<string[]> = this.playfields.asObservable();

  constructor(private http: HttpClient) { }

  get PlayfieldNames() {
    let locationsSubscription = this.http.get<string[]>("gameplay/GetAllPlayfieldNames")
      .pipe()
      .subscribe(
        L => this.playfields.next(this.mPlayfields = L),
        error => this.error = error // error path
    );

    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

    return this.playfieldsObservable;
  }

}
