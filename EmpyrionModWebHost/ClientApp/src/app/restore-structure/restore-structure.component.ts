import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { PVector3, PositionModel } from '../model/player-model';
import { FactionService } from '../services/faction.service';
import { PositionService } from '../services/position.service';
import { PlayfieldService } from '../services/playfield.service';
import { PlayfieldModel } from '../model/playfield-model';
import { StructureService } from '../services/structure.service';
import { GlobalStructureInfo } from '../model/structure-model';
import { SelectionModel } from '@angular/cdk/collections';

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
  displayedColumns = ['Select', 'id', 'name', 'playfield', 'type', 'core', 'posX', 'posY', 'posZ', 'faction', 'blocks', 'devices', 'touched_time', 'touched_name', 'add_info', 'structureName'];
  Backups: string[];
  error: any;
  mSelectedBackup: string;
  mCurrentStructure: PlayfieldGlobalStructureInfo;
  WarpData: PositionModel = {};
  Playfields: PlayfieldModel[] = [];

  structures: MatTableDataSource<PlayfieldGlobalStructureInfo> = new MatTableDataSource([]);
  selection = new SelectionModel<PlayfieldGlobalStructureInfo>(true, []);

  displayFilter: boolean = true;

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;

  constructor(
    private http: HttpClient,
    public FactionService: FactionService,
    private mStructureService: StructureService,
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
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  ngAfterViewInit() {
    this.structures.sort = this.sort;
    this.structures.paginator = this.paginator;
    this.sort.sortChange.subscribe(E => {
      console.log(E);
    });

    this.structures.filterPredicate =
      (data: PlayfieldGlobalStructureInfo, filter: string) =>
         data.id                                 .toString()   .indexOf(filter) != -1  ||
        (data.name       && data.name     .trim().toLowerCase().indexOf(filter) != -1) ||
        (data.playfield  && data.playfield.trim().toLowerCase().indexOf(filter) != -1) ||
        (data.type       && data.type     .trim().toLowerCase().indexOf(filter) != -1) ||
        (data.add_info   && data.add_info .trim().toLowerCase().indexOf(filter) != -1);
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

  /** Whether the number of selected elements matches the total number of rows. */
  isAllSelected() {
    const numSelected = this.selection.selected.length;
    if (this.structures.filteredData.length != numSelected) return false;

    const numRows = this.structures.filteredData.filter(s => this.selection.isSelected(s)).length;
    return numSelected == numRows;
  }

  /** Selects all rows if they are not all selected; otherwise clear selection. */
  masterToggle() {
    this.isAllSelected() ?
      this.selection.clear() :
      this.structures.filteredData.forEach(row => this.selection.select(row));
  }

  select(row: PlayfieldGlobalStructureInfo) {
    this.selection.clear();
    this.selection.toggle(row);
  }

  get CurrentStructure(): PlayfieldGlobalStructureInfo {
    return this.mCurrentStructure;
  }

  set CurrentStructure(aStructure: PlayfieldGlobalStructureInfo) {
    this.mCurrentStructure = aStructure;
    this.WarpData.playfield = aStructure.playfield;
    this.WarpData.pos = JSON.parse(JSON.stringify(aStructure.pos));
    this.WarpData.rot = JSON.parse(JSON.stringify(aStructure.rot));
    this.mStructureService.CurrentStructure = <GlobalStructureInfo><any>aStructure;
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
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  Create() {
    if(!this.IsSingleShip)                        this.selection.selected.forEach(S => this.CreateFromBackup(S));
    else if (this.CurrentStructure.structureName) this.CreateFromBackup(this.CurrentStructure);
    else                                          this.CreateFromEBP();
  }

  get IsSingleShip(): boolean {
    return !this.selection.selected || this.selection.selected.length == 0;
  }

  CreateFromBackup(structure: PlayfieldGlobalStructureInfo) {
    var send = JSON.parse(JSON.stringify(structure));
    send.playfield = this.WarpData.playfield;
    if (this.IsSingleShip) send.pos = this.WarpData.pos;
    else if (this.CurrentStructure) {
      send.pos.x += this.WarpData.pos.x - this.CurrentStructure.pos.x;
      send.pos.y += this.WarpData.pos.y - this.CurrentStructure.pos.y;
      send.pos.z += this.WarpData.pos.z - this.CurrentStructure.pos.z;
    }
    else {
      send.pos.x += this.WarpData.pos.x;
      send.pos.y += this.WarpData.pos.y;
      send.pos.z += this.WarpData.pos.z;
    }

    let locationsSubscription = this.http.post("Backups/CreateStructure", { backup: this.SelectedBackup, structure: send })
      .pipe()
      .subscribe(
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
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
    setTimeout(() => { locationsSubscription.unsubscribe(); }, this.IsSingleShip ? 120000 : 120000 * this.selection.selected.length);
  }

  onUploaded(aName: string) {
    let FileNameStart = aName.lastIndexOf('\\');
    this.mCurrentStructure = <any>{ name: aName.substr(FileNameStart + 1) };
  }

}
