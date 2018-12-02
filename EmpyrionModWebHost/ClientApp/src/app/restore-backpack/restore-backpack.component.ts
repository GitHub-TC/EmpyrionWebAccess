import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PlayerService } from '../services/player.service';
import { BackpackODataModel, BackpackModel } from '../model/backpack-model';
import { BehaviorSubject, Observable } from 'rxjs';

@Component({
  selector: 'app-restore-backpack',
  templateUrl: './restore-backpack.component.html',
  styleUrls: ['./restore-backpack.component.less']
})
export class RestoreBackpackComponent implements OnInit {
  CurrentBackpack: BackpackModel;
  SelectedBackpack: BackpackODataModel;
  error: any;

  private mBackpacks: BackpackODataModel[] = []; 

  public Backpacks: BehaviorSubject<BackpackODataModel[]> = new BehaviorSubject(this.mBackpacks);
  public readonly playersObservable: Observable<BackpackODataModel[]> = this.Backpacks.asObservable();

  constructor(
    private http: HttpClient,
    private PlayerService: PlayerService,
  ) { }

  ngOnInit() {
    if (!this.PlayerService.CurrentPlayer) return;

    let locationsSubscription = this.http.get<BackpackODataModel[]>("Backpacks/Backpacks/" + this.PlayerService.CurrentPlayer.SteamId)
      .subscribe(
      B => this.Backpacks.next(B.map(BB => {
        BB.timestamp = new Date(BB.timestamp);
        return BB;
      })),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  SetCurrentBackpack(aData: BackpackODataModel) {
    this.SelectedBackpack = aData;
    this.CurrentBackpack = { SteamId: aData.id, Toolbar: JSON.parse(aData.toolbarContent), Bag: JSON.parse(aData.bagContent)  };
  }

  RestoreCurrentBackpack() {
    this.http.post<BackpackODataModel>("Backpacks/SetBackpack", this.CurrentBackpack)
      .subscribe(
        error => this.error = error // error path
      );
  }

}
