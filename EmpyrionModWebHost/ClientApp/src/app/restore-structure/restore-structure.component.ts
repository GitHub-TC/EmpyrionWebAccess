import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatTableDataSource, MatSort, MatPaginator } from '@angular/material';
import { GlobalStructureInfo } from '../model/structure-model';
import { PVector3 } from '../model/player-model';
import { SelectionModel } from '@angular/cdk/collections';

interface PlayfieldGlobalStructureInfo {
  StructureName: string;
  Playfield: string;
  Id: number;
  Name: string;
  Type: string;
  Faction: number;
  Blocks: number;
  Devices: number;
  Pos: PVector3;
  Rot: PVector3;
  Core: boolean;
  Powered: boolean;
  Docked: boolean;
  Touched_time: string;
  Touched_ticks: number;
  Touched_name: string;
  Touched_id: number;
  Saved_time: string;
  Saved_ticks: number;
  Add_info: string;
}

@Component({
  selector: 'app-restore-structure',
  templateUrl: './restore-structure.component.html',
  styleUrls: ['./restore-structure.component.less']
})
export class RestoreStructureComponent implements OnInit {
  displayedColumns = ['Select', 'Id', 'Playfield', 'Name', 'Core', 'PosX', 'PosY', 'PosZ'];
  Backups: string[];
  error: any;
  mSelectedBackup: string;
  CurrentStructure: PlayfieldGlobalStructureInfo;

  structures: MatTableDataSource<PlayfieldGlobalStructureInfo> = new MatTableDataSource([]);
  displayFilter: boolean = true;

  selection = new SelectionModel<PlayfieldGlobalStructureInfo>(true, []);

  @ViewChild(MatSort) sort: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator;

  constructor(private http: HttpClient) { }

  ngOnInit() {
    let locationsSubscription = this.http.get<string[]>("Backups/GetBackups")
      .pipe()
      .subscribe(
        B => this.Backups = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  ngAfterViewInit() {
    this.structures.sort = this.sort;
    this.structures.paginator = this.paginator;
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
    const numRows = this.structures.data.length;
    return numSelected == numRows;
  }

  /** Selects all rows if they are not all selected; otherwise clear selection. */
  masterToggle() {
    this.isAllSelected() ?
      this.selection.clear() :
      this.structures.data.forEach(row => this.selection.select(row));
  }

  get SelectedBackup() {
    return this.mSelectedBackup;
  }

  set SelectedBackup(aBackup : string) {
    this.mSelectedBackup = aBackup;
    this.ReadStructuresFromBackup(aBackup);
  }

  ReadStructuresFromBackup(aBackup: string) {
    let locationsSubscription = this.http.get<PlayfieldGlobalStructureInfo[]>("Backups/ReadStructures/" + this.SelectedBackup)
      .pipe()
      .subscribe(
        S => this.structures.data = S,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }
}
