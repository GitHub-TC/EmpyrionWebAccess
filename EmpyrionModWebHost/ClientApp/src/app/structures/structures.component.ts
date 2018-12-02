import { Component, OnInit, ViewChild } from '@angular/core';
import { MatTableDataSource, MatSort } from '@angular/material';
import { StructureService } from '../services/structure.service';
import { Router } from '@angular/router';
import { GlobalStructureInfo } from '../model/structure-model';
import { PositionService } from '../services/position.service';

@Component({
  selector: 'app-structures',
  templateUrl: './structures.component.html',
  styleUrls: ['./structures.component.less']
})
export class StructuresComponent implements OnInit {
  displayedColumns = ['Id', 'Playfield', 'Name', 'Core', 'PosX', 'PosY', 'PosZ'];
  structures: MatTableDataSource<GlobalStructureInfo> = new MatTableDataSource([]);
  displayFilter: boolean = true;

  @ViewChild(MatSort) sort: MatSort;

  constructor(
    public router: Router,
    private mStructureService: StructureService,
    private mPositionService: PositionService,
  ) { }

  ngOnInit() {
    this.mStructureService.GetGlobalStructureList().subscribe(S => {
      this.structures.data = S;
    });
  }

  ngAfterViewInit() {
    this.structures.sort = this.sort;
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

  SavePosition(aStruct: GlobalStructureInfo) {
    this.mPositionService.CurrentPosition = { description: "Structure: " + aStruct.name, playfield: aStruct.playfield, entityId: aStruct.id, pos: aStruct.pos, rot: aStruct.rot };
  }
}
