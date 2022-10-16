import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PlayerModel } from '../model/player-model';
import { MatTableDataSource } from '@angular/material/table';
import { SystemInfoService } from '../services/systeminfo.service';
import { MatSort } from '@angular/material/sort';

@Component({
  selector: 'app-restore-player',
  templateUrl: './restore-player.component.html',
  styleUrls: ['./restore-player.component.less']
})
export class RestorePlayerComponent implements OnInit {
  displayedColumns = ['online', 'playerName', 'origin', 'playfield', 'posX', 'posY', 'posZ', 'lastOnline', 'onlineHours', 'entityId', 'steamId'];
  players: MatTableDataSource<PlayerModel> = new MatTableDataSource([]);
  displayFilter: boolean;
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  Backups: string[];
  mSelectedBackup: string;
  error: any;

  constructor(
    private http: HttpClient,
    public mSystemInfoService: SystemInfoService,
  ) {}

  ngOnInit() {
    let locationsSubscription = this.http.get<string[]>("Backups/GetBackups")
      .pipe()
      .subscribe(
        B => this.Backups = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  ngAfterViewInit() {
    this.players.sort = this.sort;
    this.players.filterPredicate =
      (data: PlayerModel | any, filter: string) =>
        data.playerName.trim().toLowerCase().indexOf(filter) != -1 ||
        data.playfield.trim().toLowerCase().indexOf(filter) != -1 ||
        data.solarSystem.trim().toLowerCase().indexOf(filter) != -1 ||
        data.entityId.toString().indexOf(filter) != -1 ||
        data.steamId.indexOf(filter) != -1 ||
        ('' + data.factionId).indexOf(filter) != -1;
  }

  get SelectedBackup() {
    return this.mSelectedBackup;
  }

  set SelectedBackup(aBackup: string) {
    this.mSelectedBackup = aBackup;
    this.ReadPlayerFromBackup(aBackup);
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.players.filter = filterValue;
  }

  toggleFilterDisplay(FilterInput) {
    this.displayFilter = !this.displayFilter;
    if (this.displayFilter) setTimeout(() => FilterInput.focus(), 0);
  }

  ReadPlayerFromBackup(aBackup: string) {
    let locationsSubscription = this.http.post<PlayerModel[]>("Backups/ReadPlayers", { backup: this.SelectedBackup })
      .pipe()
      .subscribe(
        S => this.players.data = S,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  RestorePlayer(aPlayer: PlayerModel) {
    let locationsSubscription = this.http.post<{ backup: string, steamId: string}>("Backups/RestorePlayer", { backup: this.SelectedBackup, steamId: (<any>aPlayer).steamId })
      .pipe()
      .subscribe(
        S => { },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }
}
