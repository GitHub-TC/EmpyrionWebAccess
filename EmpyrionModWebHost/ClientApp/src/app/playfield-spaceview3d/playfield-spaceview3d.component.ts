import { Component, } from '@angular/core';
import { Color } from 'three';
import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { PlayfieldView3D } from '../playfield-view3d/playfield-view3d';
import { GlobalStructureInfo } from '../model/structure-model';
import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-playfield-spaceview3d',
  templateUrl: './playfield-spaceview3d.component.html',
  styleUrls: ['./playfield-spaceview3d.component.less']
})
export class PlayfieldSpaceview3dComponent extends PlayfieldView3D {

  constructor(
    public mStructureService: StructureService,
    public mPlayerService: PlayerService,
  ) {
    super(mStructureService, mPlayerService);

    this.loader.load("assets/Model/BASpace.json", O => {
      this.objBA = O; this.RenderStructures();
      this.objBA.scale.x = 10;
      this.objBA.scale.y = 10;
      this.objBA.scale.z = 10;
    });
    this.loader.load("assets/Model/CV/CV.json", O => {
      this.objCV = O; this.RenderStructures();
      this.objCV.scale.x = 6;
      this.objCV.scale.y = 6;
      this.objCV.scale.z = 6;
    });
    this.loader.load("assets/Model/CVMedium/CV.json", O => {
      this.objCVMedium = O; this.RenderStructures();
      this.objCVMedium.scale.x = 2;
      this.objCVMedium.scale.y = 2;
      this.objCVMedium.scale.z = 2;
    });
    this.loader.load("assets/Model/CVSmall/CV.json", O => {
      this.objCVSmall = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/HV.json", O => {
      this.objHV = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/SV.json", O => {
      this.objSV = O; this.RenderStructures();
      this.objSV.scale.x = 10;
      this.objSV.scale.y = 10;
      this.objSV.scale.z = 10;
    });
    this.loader.load("assets/Model/Player/Player.json", O => {
      this.objPlayer = O; this.RenderPlayers();
      this.objPlayer.scale.x = 4;
      this.objPlayer.scale.y = 4;
      this.objPlayer.scale.z = 4;
    });
    this.loader.load("assets/Model/Asteroid.json", O => {
      this.objAsteroid = O; this.RenderStructures();
    });

  }

  SetStructurePos(aObject: THREE.Object3D, S: GlobalStructureInfo): any {
    aObject.position.x = S.pos.x / 5;
    aObject.position.y = S.pos.y / 5;
    aObject.position.z = S.pos.z / 5;
    aObject.rotateX(S.rot.x * this.PI360);
    aObject.rotateY(S.rot.y * this.PI360);
    aObject.rotateZ(S.rot.z * this.PI360);
  }

  SetPlayerPos(aObject: THREE.Object3D, P: PlayerModel): any {
    aObject.position.x = P.Pos.x / 5;
    aObject.position.y = P.Pos.y / 5;
    aObject.position.z = P.Pos.z / 5;
  }

  ngAfterViewInit() {
    super.ngAfterViewInit();
    setTimeout(() => this.renderer.scene.background = new Color("lightgray"), 3);
  }
}

