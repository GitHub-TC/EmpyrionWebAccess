import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PlayerService } from '../services/player.service';
import { BackpackODataModel, BackpackModel } from '../model/backpack-model';
import { PlayerModel } from '../model/player-model';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

@Component({
  selector: 'app-restore-backpack',
  templateUrl: './restore-backpack.component.html',
  styleUrls: ['./restore-backpack.component.less'],
  host: {
    '(document:keydown)': 'handleKeyboardEvents($event)'
  }
})
export class RestoreBackpackComponent implements OnInit {
  @ViewChild(YesNoDialogComponent, { static: true }) YesNo: YesNoDialogComponent;
  CurrentBackpack: BackpackModel;
  SelectedBackpack: BackpackODataModel;
  CurrentPlayer: PlayerModel;
  error: any;
  @ViewChild(MatPaginator, { static: false }) paginator: MatPaginator;

  Backpacks: MatTableDataSource<BackpackODataModel> = new MatTableDataSource([]);

  constructor(
    private http: HttpClient,
    public PlayerService: PlayerService,
  ) { }

  ngOnInit() {
    if (!this.PlayerService.CurrentPlayer) return;

    let locationsSubscription = this.http.get<BackpackODataModel[]>("Backpacks/Backpacks/" + this.PlayerService.CurrentPlayer.SteamId)
      .subscribe(
      B => {
        this.Backpacks.data = B.map(BB => {
          BB.timestamp = new Date(BB.timestamp);
          return BB;
        });
        if (this.Backpacks.data.length > 0) this.SetCurrentBackpack(this.Backpacks.data[0]);
      },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    this.PlayerService.GetCurrentPlayer().subscribe(P => this.CurrentPlayer = P);
  }

  ngAfterViewInit() {
    this.Backpacks.paginator = this.paginator;
  }

  SetCurrentBackpack(aData: BackpackODataModel) {
    this.SelectedBackpack = aData;
    this.CurrentBackpack = { SteamId: aData.id, Toolbar: JSON.parse(aData.toolbarContent), Bag: JSON.parse(aData.bagContent)  };
  }

  SlotCount(aData: BackpackODataModel) {
    return this.ItemCount(aData.toolbarContent) + this.ItemCount(aData.bagContent);
  }

  ItemCount(aContent: string): number {
    return aContent ? aContent.split("\"id\":").length - 1 : 0;
  }

  RestoreCurrentBackpack() {
    this.YesNo.openDialog({ title: "Restore backpack for", question: this.CurrentPlayer.PlayerName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.post<BackpackODataModel>("Backpacks/SetBackpack", this.CurrentBackpack)
          .subscribe(
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
    let found = this.Backpacks.data.findIndex(B => B == this.SelectedBackpack);
    if (found < this.Backpacks.data.length) this.SetCurrentBackpack(this.Backpacks.data[found + 1])
  }

  shiftUp() {
    let found = this.Backpacks.data.findIndex(B => B == this.SelectedBackpack);
    if (found > 0) this.SetCurrentBackpack(this.Backpacks.data[found - 1])
  }

}
