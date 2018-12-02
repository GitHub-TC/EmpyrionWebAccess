import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PlayerService } from '../services/player.service';
import { BackpackODataModel, BackpackModel } from '../model/backpack-model';
import { BehaviorSubject, Observable } from 'rxjs';
import { Player } from '@angular/core/src/render3/interfaces/player';
import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-restore-backpack',
  templateUrl: './restore-backpack.component.html',
  styleUrls: ['./restore-backpack.component.less'],
  host: {
    '(document:keydown)': 'handleKeyboardEvents($event)'
  }
})
export class RestoreBackpackComponent implements OnInit {
  CurrentBackpack: BackpackModel;
  SelectedBackpack: BackpackODataModel;
  CurrentPlayer: PlayerModel;
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
      B => {
        this.Backpacks.next(this.mBackpacks = B.map(BB => {
          BB.timestamp = new Date(BB.timestamp);
          return BB;
        }));
        if (this.mBackpacks.length > 0) this.SetCurrentBackpack(this.mBackpacks[0]);
      },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

    this.PlayerService.GetCurrentPlayer().subscribe(P => this.CurrentPlayer = P);
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

  handleKeyboardEvents(event: KeyboardEvent) {
    let key = event.which || event.keyCode;
    switch (key) {
      case 38: this.shiftUp(); break;
      case 40: this.shiftDown(); break;
    }
  }

  shiftDown() {
    let found = this.mBackpacks.findIndex(B => B == this.SelectedBackpack);
    if (found < this.mBackpacks.length) this.SetCurrentBackpack(this.mBackpacks[found + 1])
  }

  shiftUp() {
    let found = this.mBackpacks.findIndex(B => B == this.SelectedBackpack);
    if (found > 0) this.SetCurrentBackpack(this.mBackpacks[found - 1])
  }

}
