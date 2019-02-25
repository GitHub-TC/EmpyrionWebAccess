import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { PlayfieldService } from '../services/playfield.service';
import { PlayerModel, PVector3 } from '../model/player-model';
import { PlayfieldModel } from '../model/playfield-model';
import { GlobalStructureInfo } from '../model/structure-model';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

@Component({
  selector: 'app-playfield-view',
  templateUrl: './playfield-view.component.html',
  styleUrls: ['./playfield-view.component.less']
})
export class PlayfieldViewComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  @ViewChild("MapImage", { read: ElementRef }) MapImage: ElementRef;

  Playfields: PlayfieldModel[] = [];
  SelectedPlayfield: PlayfieldModel;
  mSelectedPlayfieldName: string = "";
  ZoomValue: number = 1;
  MapUrl: string;
  mAllPlayers: PlayerModel[];
  PlayfieldPlayers: PlayerModel[];
  mAllStructures: GlobalStructureInfo[];
  PlayfieldStructures: GlobalStructureInfo[];
  PlanetSize = [
    { w: 10000,  h: 10000 },
    { w:  4100,  h:  2500 },
    { w:  4100,  h:  2500 },
    { w:  8200,  h:  5100 },
    { w: 16400,  h: 10200 },
    { w: 32800,  h: 20200 }
  ];
  AllDisplay: string = "POn,POff,BA,CV,SV,HV";
  mDisplay: string[] = this.AllDisplay.split(",");
  mWipeData: string[] = [];
  error: any;
    SpaceImageUrl: any;

  constructor(
    private http: HttpClient,
    private mStructureService: StructureService,
    private mPlayfields: PlayfieldService,
    private mPlayerService: PlayerService,
  ) {}

  ngOnInit() {
    this.mSelectedPlayfieldName = this.mPlayfields.CurrentPlayfield ? this.mPlayfields.CurrentPlayfield.name : "";

    this.mPlayfields.PlayfieldNames.subscribe(PL => {
      this.Playfields = PL;
      this.SelectedPlayfield = this.Playfields.find(P => P.name == this.SelectedPlayfieldName);
    });
    this.mPlayerService.GetPlayers().subscribe(P => {
      this.mAllPlayers = P;
      this.PlayfieldPlayers = P.filter(p => p.Playfield == this.SelectedPlayfieldName);
    });
    this.mStructureService.GetGlobalStructureList().subscribe(S => {
      this.mAllStructures = S;
      this.SelectedPlayfieldName = this.mSelectedPlayfieldName;
    });
  }

  ngAfterViewInit() {
    this.SelectedPlayfieldName = this.mPlayfields.CurrentPlayfield ? this.mPlayfields.CurrentPlayfield.name : "";
  }

  onSelectStructure(aStructure: GlobalStructureInfo) {
    if (this.mStructureService.CurrentStructure = aStructure) return;

    this.mStructureService.CurrentStructure = aStructure;
  }

  get SelectedPlayfieldName() {
    return this.mSelectedPlayfieldName;
  }

  set SelectedPlayfieldName(aPlayfieldName: string) {
    this.mSelectedPlayfieldName = aPlayfieldName;
    if (!aPlayfieldName) return;

    this.MapUrl = "Playfield/GetPlayfieldMap/" + encodeURIComponent(this.mSelectedPlayfieldName)
    this.SelectedPlayfield = this.Playfields.find(P => P.name == aPlayfieldName);

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

    switch (aStructure.factionGroup) {
      case 0: return "magenta"; // Faction
      case 1: return "aqua"; // Privat
      case 2: return "tomato"; // Zirax
      case 3: return "tomato"; // Predator
      case 4: return "tomato"; // Prey
      case 5: return "purple"; // Admin;
      case 6: return "green";  // Talons
      case 7: return "blue";   // Polaris
      case 8: return "red";    // Aliens
      case 11: return "white"; // "Unknown";
      case 10: return "gold"; // Public
      case 12: return "white"; // "None";
      case 255: return "lightblue";   // No Core
      default: return "white";
    }
  }

  StructurZIndex(aStructure: GlobalStructureInfo) {
    return aStructure == this.mStructureService.CurrentStructure ? 100 : 10;
  }

  PlayerZIndex(aPlayer: PlayerModel) {
    return aPlayer == this.mPlayerService.CurrentPlayer ? 100 : 10;
  }


  SelectPlayer(aPlayer: PlayerModel) {
    this.mPlayerService.CurrentPlayer = aPlayer;
  }

  PlayerColor(aPlayer: PlayerModel) {
    if (aPlayer && this.mPlayerService.CurrentPlayer && aPlayer.SteamId == this.mPlayerService.CurrentPlayer.SteamId) return "yellow";
    return aPlayer.Online ? "green" : "cornflowerblue";
  }

  onUploaded() {
    this.MapUrl = "Playfield/GetPlayfieldMap/" + encodeURIComponent(this.mSelectedPlayfieldName) + '?random=' + Math.random();
  }

  UploadURL(aPlayfieldname) {
    return aPlayfieldname ? 'Playfield/UploadMapFile?PlayfieldName=' + encodeURIComponent(aPlayfieldname) : 'Playfield/UploadMapFile'
  }

  masterToggle() {
    this.mDisplay = this.isAllSelected() ? [] : this.AllDisplay.split(",");
  }

  isAllSelected() {
    return this.mDisplay.length == this.AllDisplay.split(",").length;
  }

  selectionToggle(id: string) {
    if (this.mDisplay.find(D => D == id)) this.mDisplay = this.mDisplay.filter(D => D != id);
    else                                  this.mDisplay.push(id);
  }

  isSelected(id: string) {
    return this.mDisplay.find(D => D == id);
  }

  Wipe(aType: string) {
    return this.mWipeData.find(W => W == aType);
  }

  ChangeWipe(aType: string, aSet : boolean) {
    let Found = this.mWipeData.find(W => W == aType);
    if ( Found && !aSet) this.mWipeData = this.mWipeData.filter(W => W != aType);
    if (!Found &&  aSet) this.mWipeData.push(aType);
  }

  ExecWipe() {
    this.YesNo.openDialog({ title: "Wipe " + this.mWipeData.join(", ") + " of playfield", question: this.SelectedPlayfieldName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.get("Playfield/Wipe" +
          "?Playfield=" + encodeURIComponent(this.SelectedPlayfieldName) +
          "&WipeType=" + encodeURIComponent(this.mWipeData.join(" ")))
          .subscribe(
            error => this.error = error // error path
          );
      });
  }

  WipeComplete() {
    this.YesNo.openDialog({ title: "Reset complete Playfield", question: this.SelectedPlayfieldName }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        this.http.get("Playfield/ResetPlayfield" +
          "?Playfield=" + encodeURIComponent(this.SelectedPlayfieldName))
          .subscribe(
            error => this.error = error // error path
          );
      });
  }
}
