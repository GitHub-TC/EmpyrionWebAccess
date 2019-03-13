import { Component, Input } from '@angular/core';
import * as THREE from 'three';
import { Color } from 'three';
import { PlayfieldView3D } from '../playfield-view3d/playfield-view3d';
import { PlayerModel, PVector3 } from '../model/player-model';
import { GlobalStructureInfo } from '../model/structure-model';
import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';

@Component({
  selector: 'app-playfield-planetview3d',
  templateUrl: './playfield-planetview3d.component.html',
  styleUrls: ['./playfield-planetview3d.component.less']
})
export class PlayfieldPlanetview3dComponent extends PlayfieldView3D  {
  loaderTexture: THREE.TextureLoader = new THREE.TextureLoader();
  materialPlanetMap: THREE.MeshLambertMaterial;

  radius = 200;

  mPlanetSurfacePNG: string;
  rerender: boolean;
  meshPlanetMap: THREE.Mesh;
  geometryPlanetMap: THREE.SphereBufferGeometry;
  PlanetSize = [
    { w: 10000,  h: 10000 },
    { w:  4100,  h:  2500 },
    { w:  4100,  h:  2500 },
    { w:  8200,  h:  5100 },
    { w: 16400,  h: 10200 },
    { w: 32800,  h: 20200 }
  ];

  get PlanetSurfacePNG() {
    return this.mPlanetSurfacePNG;
  }

  @Input() set PlanetSurfacePNG(aPNGUrl: string) {
    this.mPlanetSurfacePNG = aPNGUrl;

    this.loaderTexture.load(this.PlanetSurfacePNG, PNG => {
      this.materialPlanetMap = new THREE.MeshLambertMaterial({ map: PNG });

      if (this.meshPlanetMap) this.scene.removeChild(this.meshPlanetMap);
      this.meshPlanetMap = new THREE.Mesh(this.geometryPlanetMap, this.materialPlanetMap);
      this.scene.addChild(this.meshPlanetMap);

      if(this.rerender) this.renderer.render()
    });
  }

  @Input() PlayfieldSize: number;

  constructor(
    public mStructureService: StructureService,
    public mPlayerService: PlayerService,
  ) {
    super(mStructureService, mPlayerService);

    this.loader.load("assets/Model/BAPlanet.json", O => {
      this.objBA = O; this.RenderStructures();
      this.objBA.scale.x = .5;
      this.objBA.scale.y = .5;
      this.objBA.scale.z = .5;
    });
    this.loader.load("assets/Model/CV/CV.json", O => {
      this.objCV = O; this.RenderStructures();
      this.objCV.scale.x = 6;
      this.objCV.scale.y = 6;
      this.objCV.scale.z = 6;
    });
    this.loader.load("assets/Model/CVMedium/CV.json", O => {
      this.objCVMedium = O; this.RenderStructures();
      this.objCVMedium.scale.x = .5;
      this.objCVMedium.scale.y = .5;
      this.objCVMedium.scale.z = .5;
    });
    this.loader.load("assets/Model/CVSmall/CV.json", O => {
      this.objCVSmall = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/HV.json", O => {
      this.objHV = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/SV.json", O => {
      this.objSV = O; this.RenderStructures();
      //this.objSV.scale.x = 10;
      //this.objSV.scale.y = 10;
      //this.objSV.scale.z = 10;
    });
    this.loader.load("assets/Model/Player/Player.json", O => {
      this.objPlayer = O; this.RenderPlayers();
      //this.objPlayer.scale.x = 4;
      //this.objPlayer.scale.y = 4;
      //this.objPlayer.scale.z = 4;
    });
    this.loader.load("assets/Model/Asteroid.json", O => {
      this.objAsteroid = O; this.RenderStructures();
    });
  }

  SetStructurePos(aObject: THREE.Object3D, S: GlobalStructureInfo): any {
    let pos = this.NormESGPos(S.pos);
    let phi   = (Math.PI * 2 / this.PlanetSize[this.PlayfieldSize].w) * pos.x;
    let theta = this.PI2 - (this.PI2 / this.PlanetSize[this.PlayfieldSize].h) * pos.z;
    let vec = new THREE.Vector3(
      this.radius * Math.sin(theta) * Math.cos(phi),
      this.radius * Math.cos(theta),
      this.radius * Math.sin(theta) * Math.sin(phi)
    );
    aObject.position.x = vec.x;
    aObject.position.z = vec.z;
    aObject.position.y = vec.y;
    aObject.rotateY(this.PI2 - phi);
    aObject.rotateX(theta)
  }

  SetPlayerPos(aObject: THREE.Object3D, P: PlayerModel): any {
    let pos = this.NormESGPos(P.Pos);
    let phi = (Math.PI * 2 / this.PlanetSize[this.PlayfieldSize].w) * pos.x;
    let theta = this.PI2 - (this.PI2 / this.PlanetSize[this.PlayfieldSize].h) * pos.z;
    let vec = new THREE.Vector3(
      this.radius * Math.sin(theta) * Math.cos(phi),
      this.radius * Math.cos(theta),
      this.radius * Math.sin(theta) * Math.sin(phi)
    );
    aObject.position.x = vec.x;
    aObject.position.z = vec.z;
    aObject.position.y = vec.y;
    aObject.rotateY(this.PI2 - phi);
    aObject.rotateX(theta)
  }

  ngAfterViewInit() {
    super.ngAfterViewInit();
    let PoleSkip = (Math.PI / 180) * 36.87 / 2;
    this.geometryPlanetMap = new THREE.SphereBufferGeometry(this.radius, 40, 40, 0, Math.PI * 2, PoleSkip, Math.PI - 2 * PoleSkip);
    let geometryPole = new THREE.SphereBufferGeometry(this.radius - 0.5, 40, 40);
    let materialPole = new THREE.MeshLambertMaterial({ color: new Color(0xffffff) });
    let meshPole = new THREE.Mesh(geometryPole, materialPole);
    this.scene.addChild(meshPole);
  }

  NormESGPos(aPos: PVector3) : THREE.Vector3 {
    let X = aPos.x;
    let Width2 = this.PlanetSize[this.PlayfieldSize].w / 2;
    if (X < -Width2) X = 2 * this.PlanetSize[this.PlayfieldSize].w + X;

    let Width = 1024 / 2;
    X = (Width / 2) + ((Width / this.PlanetSize[this.PlayfieldSize].w) * X);

    return new THREE.Vector3(X, aPos.y, aPos.z);
  }

}
