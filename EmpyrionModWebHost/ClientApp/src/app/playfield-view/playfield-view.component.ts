import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';

import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { PositionService } from '../services/position.service';
import { FactionService } from '../services/faction.service';
import { PlayfieldService } from '../services/playfield.service';
import { PlayerModel, PVector3 } from '../model/player-model';
import { PlayfieldModel } from '../model/playfield-model';
import { GlobalStructureInfo } from '../model/structure-model';

@Component({
  selector: 'app-playfield-view',
  templateUrl: './playfield-view.component.html',
  styleUrls: ['./playfield-view.component.less']
})
export class PlayfieldViewComponent implements OnInit {
  @ViewChild("MapImage", { read: ElementRef }) MapImage: ElementRef;

  Playfields: PlayfieldModel[];
  SelectedPlayfield: PlayfieldModel;
  mSelectedPlayfieldName: string = "";
  mAllPlayers: PlayerModel[];
  PlayfieldPlayers: PlayerModel[];
  mAllStructures: GlobalStructureInfo[];
  SelectedStructure: GlobalStructureInfo;
  PlayfieldStructures: GlobalStructureInfo[];
  PlanetSize = [
    { w: 10000,  h: 10000 },
    { w:  4100,  h:  2500 },
    { w:  4100,  h:  2500 },
    { w:  8200,  h:  5100 },
    { w: 16400,  h: 10200 },
    { w: 32800,  h: 20200 }
  ];

  constructor(
    private mStructureService: StructureService,
    private mPlayfields: PlayfieldService,
    private mPlayerService: PlayerService,
  ) { }

  ngOnInit() {
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
    this.mPlayerService.GetPlayers().subscribe(P => {
      this.mAllPlayers = P;
      this.PlayfieldPlayers = P.filter(p => p.Playfield == this.SelectedPlayfieldName);
    });
    this.mStructureService.GetGlobalStructureList().subscribe(S => {
      this.mAllStructures = S;
      this.SelectedPlayfieldName = this.mSelectedPlayfieldName;
    });

  }

  onSelectStructure(aStructure: GlobalStructureInfo) {
    this.SelectedStructure = aStructure;
    if (!this.SelectedStructure) return;

    this.PlayfieldStructures = this.mAllStructures.filter(S => S.playfield == aStructure.playfield);
  }


  get SelectedPlayfieldName() {
    return this.mSelectedPlayfieldName;
  }

  set SelectedPlayfieldName(aPlayfieldName: string) {
    this.mSelectedPlayfieldName = aPlayfieldName;
    if (!aPlayfieldName) return;

    this.SelectedPlayfield   = this.Playfields.find(P => P.name == aPlayfieldName);

    this.PlayfieldStructures = this.mAllStructures.filter(S => S.playfield == aPlayfieldName);
    this.PlayfieldPlayers    = this.mAllPlayers.filter(P => P.Playfield == this.SelectedPlayfieldName);
  }

  CalcLeft(aPos: PVector3) {
    if (!this.MapImage) return "0px";

    if (this.SelectedPlayfield.isPlanet) {
      let X = aPos.x;
      let Width2 = this.PlanetSize[this.SelectedPlayfield.size].w / 2;
      if (X < -Width2) X = 2 * this.PlanetSize[this.SelectedPlayfield.size].w + X;

      let Width = this.MapImage.nativeElement.clientWidth / 2;
      return ((Width / 2) + ((Width / this.PlanetSize[this.SelectedPlayfield.size].w) * X)) + "px";
    }
    else {
      let Width = this.MapImage.nativeElement.clientWidth / 2;
      return (Width - ((Width / this.PlanetSize[this.SelectedPlayfield.size].w) * aPos.x)) + "px";
    }
  }

  CalcTop(aPos: PVector3) {
    if (!this.MapImage) return "0px";

    let Height = this.MapImage.nativeElement.clientHeight / 2;
    return (Height - ((Height / this.PlanetSize[this.SelectedPlayfield.size].h) * aPos.z)) + "px";
  }

  SelectStructure(aStructur: GlobalStructureInfo) {
    this.mStructureService.CurrentStructure = aStructur;
  }

  StructurIcon(aStructure: GlobalStructureInfo) {
    switch (aStructure.TypeName) {
      case "BA": return "store_mall_directory";
      case "CV": return "local_play";
      case "SV": return "flight";
      case "HV": return "directions_car";
      default: return "place";
    }
  }

  StructurColor(aStructure: GlobalStructureInfo) {
    if (aStructure && this.mStructureService.CurrentStructure && aStructure.id == this.mStructureService.CurrentStructure.id) return "yellow";
    if (aStructure.factionGroup == 5 || (aStructure.CoreName && aStructure.CoreName.includes("Admin"))) return "purple";

    if (aStructure.factionGroup ==   2) return "tomato"; // Zirax
    if (aStructure.factionGroup ==   6) return "green";  // Talons
    if (aStructure.factionGroup ==   7) return "blue";   // Polaris
    if (aStructure.factionGroup ==   8) return "red";    // Aliens
    if (aStructure.factionGroup == 255) return "white";  // No Core

    return aStructure.factionId > 0 ? "magenta" : "red";
  }

  StructurZIndex(aStructure: GlobalStructureInfo) {
    return aStructure == this.SelectedStructure ? 100 : 10;
  }

  SelectPlayer(aPlayer: PlayerModel) {
    this.mPlayerService.CurrentPlayer = aPlayer;
  }

  PlayerColor(aPlayer: PlayerModel) {
    if (aPlayer && this.mPlayerService.CurrentPlayer && aPlayer.SteamId == this.mPlayerService.CurrentPlayer.SteamId) return "yellow";
    return aPlayer.Online ? "green" : "cornflowerblue";
  }

}
