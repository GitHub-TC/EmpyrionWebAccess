import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { MatTableDataSource, MatPaginator } from '@angular/material';
import { PlayerModel } from '../model/player-model';
import { FactionService } from '../services/faction.service';
import { PositionService } from '../services/position.service';

export class HistoryBookOfStructures {
  timestamp: Date;
  playfield: string;
  entityId: number;
  posX: number;
  posY: number;
  posZ: number;
  changed: string;
}

export class HistoryBookOfPlayers {
  timestamp: Date;
  playfield: string;
  steamId: string;
  posX: number;
  posY: number;
  posZ: number;
  online: boolean;
  changed: string;
}

export class TimeFrameData {
  t: Date;
  s: HistoryBookOfStructures;
  p: HistoryBookOfPlayers;
}


@Component({
  selector: 'app-history-book-of',
  templateUrl: './history-book-of.component.html',
  styleUrls: ['./history-book-of.component.less']
})
export class HistoryBookOfComponent implements OnInit {
  FromTime: Date = new Date();
  ToTime:   Date = new Date();
  Distance: number = 1000;
  error: any;
  HideOnlyVisited: boolean = true;
  HideFirstRead: boolean = true;
  HideOnlyPositionChanged: boolean = true;

  displayedColumns = [
    'Timestamp', "Type", 'Name', 'Distance', 'PosX', 'PosY', 'PosZ', 'Playfield', 'TypeName', 'CoreName', 'FactionName', 'FactionGroup',
    'dockedShips', 'classNr', 'cntLights', 'cntTriangles', 'cntBlocks', 'cntDevices', 'powered', 'pilotId',
    'Credits', 'Kills', 'Died', 'Exp', 'Upgrade', 'Permission', 'LastVisited'
  ];
  History: MatTableDataSource<TimeFrameData> = new MatTableDataSource([]);
  @ViewChild(MatPaginator) paginator: MatPaginator;
  mPlayers: PlayerModel[] = [];

  constructor(
    private http: HttpClient,
    public mStructureService: StructureService,
    private mPositionService: PositionService,
    private mFactionService: FactionService,
    public mPlayerService: PlayerService,
  ) { }

  ngOnInit() {
    this.FromTime.setDate(this.FromTime.getDate() - 7);
    this.mPlayerService.GetPlayers().subscribe(P => this.mPlayers = P);
  }

  ngAfterViewInit() {
    this.History.paginator = this.paginator;
  }

  applyFilter(filterValue: string) {
    filterValue = filterValue.trim(); // Remove whitespace
    filterValue = filterValue.toLowerCase(); // Datasource defaults to lowercase matches
    this.History.filter = filterValue;
  }


  WhatHappendAroundPlayer() {
    let locationsSubscription = this.http.post<TimeFrameData[]>("HistoryBook/WhatHappendAroundPlayer",
      {
        SteamId: this.mPlayerService.CurrentPlayer.SteamId,
        FromDateTime: this.FromTime,
        ToDateTime: this.ToTime,
        Distance: this.Distance,
        HideOnlyVisited: this.HideOnlyVisited,
        HideFirstRead: this.HideFirstRead,
        HideOnlyPositionChanged: this.HideOnlyPositionChanged
      })
      .pipe()
      .subscribe(
        H => this.SetHistoryData(H),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 60000);
  }

  WhatHappendAroundStructure() {
    let locationsSubscription = this.http.post<TimeFrameData[]>("HistoryBook/WhatHappendAroundStructure",
      {
        Id: this.mStructureService.CurrentStructure.id,
        FromDateTime: this.FromTime,
        ToDateTime: this.ToTime,
        Distance: this.Distance,
        HideOnlyVisited: this.HideOnlyVisited,
        HideFirstRead: this.HideFirstRead,
        HideOnlyPositionChanged: this.HideOnlyPositionChanged
      })
      .pipe()
      .subscribe(
        H => this.SetHistoryData(H),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  SetHistoryData(H) {
    this.History.data = H.map(h => {
      let R = <any>h;
      R.t = new Date(R.t);

      if (h.s) { R = Object.assign(R, h.s); if (h.s.changed) R = Object.assign(R, JSON.parse(h.s.changed)); }
      if (h.p) { R = Object.assign(R, h.p); if (h.p.changed) R = Object.assign(R, JSON.parse(h.p.changed)); }

      if (R.LastVisitedUTC) R.LastVisitedUTC = new Date(R.LastVisitedUTC);

      if (R.CoreType) R.CoreName = ["None", "Player", "Admin", "Alien", "AlienAdmin", "NoFaction"][R.CoreType];
      if (R.Type) R.TypeName = ["Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel"][R.Type];
      if (R.FactionRole) R.FactionRoleName = ["Owner", "Admin", "Member"][R.FactionRole];

      if (R.FactionGroup == 1) {
        let Found = this.mPlayers.find(P => P.EntityId == R.FactionId);
        R.FactionName = Found ? Found.PlayerName : "" + R.FactionId;
      }
      else if (R.FactionId) {
        let Faction = this.mFactionService.GetFaction(R.FactionId);
        R.FactionName = Faction ? Faction.Abbrev : "";
      }
      if (R.FactionGroup) R.FactionGroupName = this.mFactionService.GetFactionGroup(R.FactionGroup);

      // Old Values
      if (R.CoreTypeOld) R.CoreNameOld = ["None", "Player", "Admin", "Alien", "AlienAdmin", "NoFaction"][R.CoreTypeOld];
      if (R.TypeOld) R.TypeNameOld = ["Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel"][R.TypeOld];
      if (R.FactionRoleOld) R.FactionRoleNameOld = ["Owner", "Admin", "Member"][R.FactionRoleOld];

      if (R.FactionGroupOld == 1) {
        let Found = this.mPlayers.find(P => P.EntityId == R.FactionIdOld);
        R.FactionNameOld = Found ? Found.PlayerName : "" + R.FactionIdOld;
      }
      else if (R.FactionIdOld) {
        let Faction = this.mFactionService.GetFaction(R.FactionIdOld);
        R.FactionNameOld = Faction ? Faction.Abbrev : "";
      }
      if (R.FactionGroupOld) R.FactionGroupNameOld = this.mFactionService.GetFactionGroup(R.FactionGroupOld);

      return R
    });
  }

  SavePosition(aData) {
    this.mPositionService.CurrentPosition = {
      description: "History: " + aData.name,
      playfield: aData.playfield,
      pos: { x: aData.posX, y: aData.posY, z: aData.posZ },
      rot: { x: 0, y: 0, z: 0 }
    };
  }


}
