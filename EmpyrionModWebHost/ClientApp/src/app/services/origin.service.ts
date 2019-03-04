import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OriginService {
  mOrigins: any = []

  private origins: BehaviorSubject<any> = new BehaviorSubject(this.mOrigins);
  public readonly originsObservable: Observable<any> = this.origins.asObservable();
  error: any;

  constructor(private http: HttpClient) {
  }

  GetOrigins(): Observable<any> {
    if (!this.mOrigins || !<any>(this.mOrigins).length) this.ReadOrigins();
    return                                              this.originsObservable;
  }

  ReadOrigins(): any {
    let locationsSubscription = this.http.get<any>("Sectors/Origins")
      .subscribe(
        O => this.origins.next(this.mOrigins = O),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

}
