import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatTableDataSource, MatSort, MatPaginator } from '@angular/material';
import { PVector3, PositionModel } from '../model/player-model';
import { FactionService } from '../services/faction.service';
import { PositionService } from '../services/position.service';
import { PlayfieldService } from '../services/playfield.service';
import { PlayfieldModel } from '../model/playfield-model';

interface PlayfieldGlobalStructureInfo {
  structureName: string;
  playfield: string;
  id: number;
  name: string;
  type: string;
  faction: number;
  blocks: number;
  devices: number;
  pos: PVector3;
  rot: PVector3;
  core: boolean;
  powered: boolean;
  docked: boolean;
  touched_time: string;
  touched_ticks: number;
  touched_name: string;
  touched_id: number;
  saved_time: string;
  saved_ticks: number;
  add_info: string;
}

@Component({
  selector: 'app-restore-structure',
  templateUrl: './restore-structure.component.html',
  styleUrls: ['./restore-structure.component.less']
})
export class RestoreStructureComponent implements OnInit {
  displayedColumns = ['id', 'name', 'playfield', 'type', 'core', 'posX', 'posY', 'posZ', 'faction', 'blocks', 'devices', 'touched_time', 'touched_name', 'add_info', 'structureName'];
  Backups: string[];
  error: any;
  mSelectedBackup: string;
  mCurrentStructure: PlayfieldGlobalStructureInfo;
  WarpData: PositionModel = {};
  Playfields: PlayfieldModel[] = [];

  structures: MatTableDataSource<PlayfieldGlobalStructureInfo> = new MatTableDataSource([]);
  displayFilter: boolean = true;

  @ViewChild(MatSort) sort: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator;

  constructor(
    private http: HttpClient,
    public FactionService: FactionService,
    public mPositionService: PositionService,
    private mPlayfields: PlayfieldService,
  ) { }

  ngOnInit() {
    this.setToZeroPosition();
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
    let locationsSubscription = this.http.get<string[]>("Backups/GetBackups")
      .pipe()
      .subscribe(
        B => {
          this.Backups = B;
          this.Backups.unshift("### Current Savegame ###");
        },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  ngAfterViewInit() {
    this.structures.sort = this.sort;
    this.structures.paginator = this.paginator;
    this.sort.sortChange.subscribe(E => {
      console.log(E);
    });
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.structures.filter = filterValue;
  }

  toggleFilterDisplay(FilterInput) {
    this.displayFilter = !this.displayFilter;
    if (this.displayFilter) setTimeout(() => FilterInput.focus(), 0);
  }

  get CurrentStructure(): PlayfieldGlobalStructureInfo {
    return this.mCurrentStructure;
  }

  set CurrentStructure(aStructure: PlayfieldGlobalStructureInfo) {
    this.mCurrentStructure = aStructure;
    this.WarpData.playfield = aStructure.playfield;
    this.WarpData.pos = JSON.parse(JSON.stringify(aStructure.pos));
    this.WarpData.rot = JSON.parse(JSON.stringify(aStructure.rot));
  }

  copyPosition() {
    this.WarpData.playfield = this.mPositionService.CurrentPosition.playfield;
    this.WarpData.pos = JSON.parse(JSON.stringify(this.mPositionService.CurrentPosition.pos));
    this.WarpData.rot = JSON.parse(JSON.stringify(this.mPositionService.CurrentPosition.rot));
  }

  setToZeroPosition() {
    this.WarpData.pos = { x: 0, y: 0, z: 0 };
    this.WarpData.rot = { x: 0, y: 0, z: 0 };
  }

  get SelectedBackup() {
    return this.mSelectedBackup;
  }

  set SelectedBackup(aBackup : string) {
    this.mSelectedBackup = aBackup;
    this.ReadStructuresFromBackup(aBackup);
  }

  ReadStructuresFromBackup(aBackup: string) {
    let locationsSubscription = this.http.post<PlayfieldGlobalStructureInfo[]>("Backups/ReadStructures", { backup: this.SelectedBackup })
      .pipe()
      .subscribe(
        S => this.structures.data = S,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  Create() {
    if (this.CurrentStructure.structureName) this.CreateFromBackup();
    else                                     this.CreateFromEBP();
  }

  CreateFromBackup() {
    var send = JSON.parse(JSON.stringify(this.CurrentStructure));
    send.playfield = this.WarpData.playfield;
    send.pos = this.WarpData.pos;
    let locationsSubscription = this.http.post("Backups/CreateStructure", { backup: this.SelectedBackup, structure: send })
      .pipe()
      .subscribe(
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  CreateFromEBP() {
    var send = JSON.parse(JSON.stringify(this.CurrentStructure));
    send.playfield = this.WarpData.playfield;
    send.pos = this.WarpData.pos;
    let locationsSubscription = this.http.post("Structure/CreateStructure", send)
      .pipe()
      .subscribe(
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  onUploaded(aName: string) {
    let FileNameStart = aName.lastIndexOf('\\');
    this.mCurrentStructure = <any>{ name: aName.substr(FileNameStart + 1) };
  }

}
