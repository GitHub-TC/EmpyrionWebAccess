import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';
import { MatPaginator, MatTableDataSource } from '@angular/material';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { BlueprintResources } from '../factory/factory.component';

interface FactoryItems extends BlueprintResources {
  steamId: string;
  timestamp: Date;
  content: string;
  inProduction: string;
  produced: string;
}

@Component({
  selector: 'app-restore-factoryitems',
  templateUrl: './restore-factoryitems.component.html',
  styleUrls: ['./restore-factoryitems.component.less'],
  host: {
    '(document:keydown)': 'handleKeyboardEvents($event)'
  }
})
export class RestoreFactoryItemsComponent implements OnInit {
  @ViewChild(YesNoDialogComponent, { static: true }) YesNo: YesNoDialogComponent;
  CurrentFactoryItems: FactoryItems;
  SelectedFactoryItems: FactoryItems;
  CurrentPlayer: PlayerModel;
  error: any;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  FactoryItems: MatTableDataSource<FactoryItems> = new MatTableDataSource([]);

  constructor(
    private http: HttpClient,
    public PlayerService: PlayerService,
  ) { }

  ngOnInit() {
    if (!this.PlayerService.CurrentPlayer) return;

    let locationsSubscription = this.http.get<FactoryItems[]>("Factory/FactoryItems/" + this.PlayerService.CurrentPlayer.SteamId)
      .subscribe(
      B => {
        this.FactoryItems.data = B.map(BB => {
          BB.timestamp       = new Date(BB.timestamp);
          BB.itemStacks      = JSON.parse(BB.content);
          BB.produced        = BB.produced;
          BB.replaceExisting = true;
          BB.playerId        = this.CurrentPlayer.EntityId;
          return BB;
        });
        if (this.FactoryItems.data.length > 0) this.SetCurrentFactoryItems(this.FactoryItems.data[0]);
      },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    this.PlayerService.GetCurrentPlayer().subscribe(P => this.CurrentPlayer = P);
  }

  ngAfterViewInit() {
    this.FactoryItems.paginator = this.paginator;
  }

  SetCurrentFactoryItems(aData: FactoryItems) {
    this.SelectedFactoryItems = aData;
    this.CurrentFactoryItems = aData;
  }

  SlotCount(aData: FactoryItems) {
    return aData.itemStacks ? aData.itemStacks.length : 0;
  }

  ItemsCount(aData: FactoryItems) {
    return aData.itemStacks ? aData.itemStacks.map(I => I.count).reduce((S, I) => S + I) : 0;
  }

  RestoreCurrentFactoryItems() {
    this.YesNo.openDialog({ title: "Restore FactoryItems for", question: this.CurrentPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.post('Factory/SetBlueprintResources', this.CurrentFactoryItems)
          .pipe()
          .subscribe(
            B => { },
            error => this.error = error // error path
          );
      });
  }

  handleKeyboardEvents(event: KeyboardEvent) {
    let key = event.which || event.keyCode;
    switch (key) {
      case 38: this.shiftUp(); break;
      case 40: this.shiftDown(); break;
    }
  }

  shiftDown() {
    let found = this.FactoryItems.data.findIndex(B => B == this.SelectedFactoryItems);
    if (found < this.FactoryItems.data.length) this.SetCurrentFactoryItems(this.FactoryItems.data[found + 1])
  }

  shiftUp() {
    let found = this.FactoryItems.data.findIndex(B => B == this.SelectedFactoryItems);
    if (found > 0) this.SetCurrentFactoryItems(this.FactoryItems.data[found - 1])
  }

}
