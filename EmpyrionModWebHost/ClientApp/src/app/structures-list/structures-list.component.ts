import { Component, OnInit, ViewChild, Input, Output, EventEmitter } from '@angular/core';
import { MatTableDataSource, MatSort, MatPaginator } from '@angular/material';
import { HttpClient } from '@angular/common/http';
import { StructureService } from '../services/structure.service';
import { GlobalStructureInfo } from '../model/structure-model';
import { PositionService } from '../services/position.service';
import { SelectionModel } from '@angular/cdk/collections';
import { FactionService } from '../services/faction.service';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { PlayerService } from '../services/player.service';
import { FactionSelectDialogComponent } from '../faction-select-dialog/faction-select-dialog.component';
import { UserRole } from '../model/user';
import { RoleService } from '../services/role.service';

@Component({
  selector: 'app-structures-list',
  templateUrl: './structures-list.component.html',
  styleUrls: ['./structures-list.component.less']
})
export class StructuresListComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  @ViewChild(FactionSelectDialogComponent) FactionSelect: FactionSelectDialogComponent;
  @Output() SelectStructure = new EventEmitter<GlobalStructureInfo>();

  displayedColumns = ['Select', 'id', 'playfield', 'name', 'TypeName', 'CoreName', 'FactionName', 'FactionGroup', 'PosX', 'PosY', 'PosZ', 'RotX', 'RotY', 'RotZ', 'dockedShips', 'classNr', 'cntLights', 'cntTriangles', 'cntBlocks', 'cntDevices', 'fuel', 'powered', 'pilotId'];
  structures: MatTableDataSource<GlobalStructureInfo> = new MatTableDataSource([]);

  selection = new SelectionModel<GlobalStructureInfo>(true, []);

  @ViewChild(MatSort) sort: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator;

  error: any;
  mAllStructures: GlobalStructureInfo[];
  mSelectedPlayfield: string;
  UserRole = UserRole;

  constructor(
    private http: HttpClient,
    public PlayerService: PlayerService,
    private mStructureService: StructureService,
    private mPositionService: PositionService,
    public FactionService: FactionService,
    public role: RoleService,
  ) { }

  ngOnInit() {
    if (!this.mAllStructures) this.mStructureService.GetGlobalStructureList()
      .subscribe(S => {
        setTimeout(() => {
          this.mAllStructures = S;
          this.SelectedPlayfield = this.mSelectedPlayfield;
        }, 10);
      });
  }

  @Input()
  set Structures(aStructures: GlobalStructureInfo[]) {
    this.mAllStructures = this.structures.data = aStructures;
  }

  @Input() 
  set SelectedPlayfield(aPlayfield: string) {
    this.mSelectedPlayfield = aPlayfield;

    if (this.mAllStructures) this.structures.data = this.mAllStructures.filter(s => !this.mSelectedPlayfield || s.playfield == this.mSelectedPlayfield)
  }

  ngAfterViewInit() {
    this.structures.sort = this.sort;
    this.structures.paginator = this.paginator;
    this.structures.sortingDataAccessor = (D, H) => typeof (D[H]) === "string" ? ("" + D[H]).toLowerCase() : D[H];
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.structures.filter = filterValue;
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

  select(row: GlobalStructureInfo) {
    this.mStructureService.CurrentStructure = row;
    this.selection.clear();
    this.selection.toggle(row);
    this.SelectStructure.emit(row);
  }

  SavePosition(aStruct: GlobalStructureInfo) {
    this.mPositionService.CurrentPosition = { description: "Structure: " + aStruct.name, playfield: aStruct.playfield, entityId: aStruct.id, pos: aStruct.pos, rot: aStruct.rot };
  }

  ReloadStructures() {
    this.mStructureService.ReloadStructures();
  }

  Destroy() {
    this.YesNo.openDialog({ title: "Destroy", question: this.selection.selected.length + " structures?" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.post("Structure/DeleteStructures", this.selection.selected.map(S => <any>{ id: S.id, playfield: S.playfield }))
          .pipe()
          .subscribe(
            S => {},
            error => this.error = error // error path
          );
      });
  }

  ChangeFaction() {
    this.FactionSelect.openDialog("Set faction of " + this.selection.selected.length + " structures?").afterClosed().subscribe(
      (SelectedFaction: string) => {
        if (!SelectedFaction) return;
        this.http.post("Structure/SetFactionOfStuctures", { FactionAbbrev: SelectedFaction, EntityIds: this.selection.selected.map(S => S.id) })
          .pipe()
          .subscribe(
            S => { },
            error => this.error = error // error path
          );
      });
  }
}
