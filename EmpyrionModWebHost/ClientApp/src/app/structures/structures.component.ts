import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource, MatSort, MatPaginator } from '@angular/material';
import { StructureService } from '../services/structure.service';
import { Router } from '@angular/router';
import { GlobalStructureInfo } from '../model/structure-model';
import { PositionService } from '../services/position.service';
import { SelectionModel } from '@angular/cdk/collections';
import { FactionService } from '../services/faction.service';

@Component({
  selector: 'app-structures',
  templateUrl: './structures.component.html',
  styleUrls: ['./structures.component.less']
})
export class StructuresComponent implements OnInit {
  displayedColumns = ['Select', 'Id', 'Playfield', 'Name', 'Core', 'PosX', 'PosY', 'PosZ', 'RotX', 'RotY', 'RotZ', 'dockedShips', 'classNr', 'cntLights', 'cntTriangles', 'cntBlocks', 'cntDevices', 'fuel', 'powered', 'factionId', 'factionGroup', 'type', 'pilotId'];
  structures: MatTableDataSource<GlobalStructureInfo> = new MatTableDataSource([]);
  displayFilter: boolean = true;

  selection = new SelectionModel<GlobalStructureInfo>(true, []);

  @ViewChild(MatSort) sort: MatSort;
  @ViewChild(MatPaginator) paginator: MatPaginator;

  constructor(
    public router: Router,
    private mStructureService: StructureService,
    private mPositionService: PositionService,
    public FactionService: FactionService,
  ) { }

  ngOnInit() {
    this.mStructureService.GetGlobalStructureList().subscribe(S => {
      this.structures.data = S;
    });
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


  toggleFilterDisplay(FilterInput) {
    this.displayFilter = !this.displayFilter;
    if (this.displayFilter) setTimeout(() => FilterInput.focus(), 0);
  }

  SavePosition(aStruct: GlobalStructureInfo) {
    this.mPositionService.CurrentPosition = { description: "Structure: " + aStruct.name, playfield: aStruct.playfield, entityId: aStruct.id, pos: aStruct.pos, rot: aStruct.rot };
  }

  ReloadStructures() {
    this.mStructureService.ReloadStructures();
  }

  Destroy() {
  }

  SetToAdmin() {
  }

  SetToAlien() {
  }

  ChangeFaction() {
  }
}
