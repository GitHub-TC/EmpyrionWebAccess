import { Component, OnInit, ViewChild, Input, Output, EventEmitter } from '@angular/core';
import { MatInput } from '@angular/material/input';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
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
import { FactionModel } from '../model/faction-model';

@Component({
  selector: 'app-structures-list',
  templateUrl: './structures-list.component.html',
  styleUrls: ['./structures-list.component.less']
})
export class StructuresListComponent implements OnInit {
  @ViewChild(YesNoDialogComponent, { static: true }) YesNo: YesNoDialogComponent;
  @ViewChild(FactionSelectDialogComponent, { static: true }) FactionSelect: FactionSelectDialogComponent;
  @Output() SelectStructure = new EventEmitter<GlobalStructureInfo>();

  displayedColumns = ['Select', 'id', 'playfield', 'solarSystem', 'name', 'TypeName', 'CoreName', 'FactionName', 'FactionGroup', 'PosX', 'PosY', 'PosZ', 'RotX', 'RotY', 'RotZ', 'dockedShips', 'classNr', 'cntLights', 'cntTriangles', 'cntBlocks', 'cntDevices', 'fuel', 'powered', 'pilotId'];
  structures: MatTableDataSource<GlobalStructureInfo> = new MatTableDataSource([]);

  selection = new SelectionModel<GlobalStructureInfo>(true, []);

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatInput, { static: true }) FilterInput: MatInput;

  error: any;
  mAllStructures: GlobalStructureInfo[];
  mSelectedPlayfield: string;
  UserRole = UserRole;
  mFactions: FactionModel[] = [];

  constructor(
    private http: HttpClient,
    public PlayerService: PlayerService,
    public mStructureService: StructureService,
    private mPositionService: PositionService,
    public mFactionService: FactionService,
    public role: RoleService,
  ) {

    this.mFactionService.GetFactions().subscribe(F => this.mFactions = F);
  }

  ngOnInit() {
    if (!this.mAllStructures) this.mStructureService.GetGlobalStructureList()
      .subscribe(S => {
        setTimeout(() => {
          this.mAllStructures = S;
          this.SelectedPlayfield = this.mSelectedPlayfield;

          if (this.mStructureService.FilterPreset) {
            this.FilterInput.value = this.mStructureService.FilterPreset;
            this.applyFilter(this.mStructureService.FilterPreset);
            this.mStructureService.FilterPreset = null;
          }
        }, 10);
      });
  }

  @Input()
  set Structures(aStructures: GlobalStructureInfo[]) {
    this.mAllStructures = this.structures.data = aStructures;

    if (this.mStructureService.FilterPreset) {
      this.FilterInput.value = this.mStructureService.FilterPreset;
      this.applyFilter(this.mStructureService.FilterPreset);
      this.mStructureService.FilterPreset = null;
    }
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

    this.structures.filterPredicate =
      (data: GlobalStructureInfo, filter: string) =>
        data.id       .toString().indexOf(filter) != -1 ||
        data.name     .trim().toLowerCase().indexOf(filter) != -1 ||
        data.playfield.trim().toLowerCase().indexOf(filter) != -1 ||
        data.solarSystemName.trim().toLowerCase().indexOf(filter) != -1 ||
        data.coreType .toString().indexOf(filter) != -1 ||
        this.Faction(data) && this.Faction(data).Abbrev.trim().toLowerCase().indexOf(filter) != -1 ||
        ('' + data.factionId).indexOf(filter) != -1;
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.structures.filter = filterValue;
  }

  Faction(model: GlobalStructureInfo) {
    return model ? this.mFactions.find(F => F.FactionId == model.factionId) : new FactionModel();
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

  EntityRepair() {
    this.selection.selected.map(S => {
      this.http.post("Structure/CallEntity", { Playfield: S.playfield, EntityId: S.id, Command: "-repair"})
        .pipe()
        .subscribe(
          SR => { },
          error => this.error = error // error path
        );
    });
  }
}
