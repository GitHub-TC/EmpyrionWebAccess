import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { SceneDirective } from '../three-js/objects/scene.directive';
import * as THREE from 'three';
import { Color, Object3D } from 'three';
import { GlobalStructureInfo } from '../model/structure-model';

@Component({
  selector: 'app-playfield-spaceview3d',
  templateUrl: './playfield-spaceview3d.component.html',
  styleUrls: ['./playfield-spaceview3d.component.less']
})
export class PlayfieldSpaceview3dComponent implements OnInit {
  @ViewChild(WebGLRendererComponent) renderer;
  @ViewChild(SceneDirective) scene;

  loader: THREE.ObjectLoader = new THREE.ObjectLoader();
  mStructures: GlobalStructureInfo[];

  get Structures() { return this.mStructures; }
  @Input() set Structures(aStructures: GlobalStructureInfo[]) { this.mStructures = aStructures; this.rerender = true; this.RenderStructures(); }

  public rotationX = 0.0;
  public rotationY = 0.0;
  public rotationZ = 0.0;

  public cameraX = 250.0;
  public cameraY = 250.0;
  public cameraZ = 250.0;

  public cameraFar = 10000.0;
  public cameraNear = 1.0;
  public cameraFovr = 60.0;

  rerender: boolean;
  objBASpace:  THREE.Object3D;
  objCV:       THREE.Object3D;
  objCVSmall:  THREE.Object3D;
  objCVMedium: THREE.Object3D;
  objHV:       THREE.Object3D;
  objSV:       THREE.Object3D;

  PI2: number = Math.PI / 180;
  StructureObjectView: Array<{ id: number, object: THREE.Object3D }> = [];

  constructor() {
    this.loader.load("assets/Model/BASpace.json", O => {
      this.objBASpace = O; this.RenderStructures();
      this.objBASpace.scale.x = 10;
      this.objBASpace.scale.y = 10;
      this.objBASpace.scale.z = 10;
    });
    this.loader.load("assets/Model/CV.json", O => {
      this.objCV = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/CVMedium.json", O => {
      this.objCVMedium = O; this.RenderStructures();
      this.objCVMedium.scale.x = 2;
      this.objCVMedium.scale.y = 2;
      this.objCVMedium.scale.z = 2;
    });
    this.loader.load("assets/Model/CVSmall.json", O => {
      this.objCVSmall = O; this.RenderStructures();
      this.objCVSmall.scale.x = 2;
      this.objCVSmall.scale.y = 2;
      this.objCVSmall.scale.z = 2;
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
  }

  ngOnInit() {
  }

  RenderStructures(): any {
    if (!this.rerender || !this.objBASpace || !this.objCVSmall || !this.objHV || !this.objSV || !this.Structures || !this.Structures.length) return;

    this.rerender = false;
    this.StructureObjectView.map(O => this.scene.removeChild(O.object));
    this.StructureObjectView = [];

    this.Structures.map(S => {
      let insertStructure : THREE.Object3D;
      switch (S.TypeName) {
        case "BA": insertStructure = this.objBASpace; break;
        case "CV": insertStructure =
            S.classNr > 10 ? this.objCV :
            S.classNr >  5 ? this.objCVMedium
                           : this.objCVSmall; break;
        case "HV": insertStructure = this.objHV; break;
        case "SV": insertStructure = this.objSV; break;
      }

      if (insertStructure) {

        insertStructure = insertStructure.clone();
        insertStructure.position.x = S.pos.x / 5;
        insertStructure.position.y = S.pos.y / 5;
        insertStructure.position.z = S.pos.z / 5;

        this.StructureObjectView.push({ id: S.id, object: insertStructure });

        this.scene.addChild(insertStructure);
      }
    });

    this.renderer.render();
  }

  ngAfterViewInit() {
    setTimeout(() => this.scene.InitChilds(), 1);
    setTimeout(() => { this.rerender = true; this.renderer.startRendering(); }, 2);
  }
}
