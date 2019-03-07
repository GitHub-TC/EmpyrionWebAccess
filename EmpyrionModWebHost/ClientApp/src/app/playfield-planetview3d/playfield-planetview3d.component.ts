import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { SceneDirective } from '../three-js/objects/scene.directive';
import * as THREE from 'three';
import { Color } from 'three';

@Component({
  selector: 'app-playfield-planetview3d',
  templateUrl: './playfield-planetview3d.component.html',
  styleUrls: ['./playfield-planetview3d.component.less']
})
export class PlayfieldPlanetview3dComponent implements OnInit {
  @ViewChild(WebGLRendererComponent) renderer;
  @ViewChild(SceneDirective) scene;

  loader: THREE.TextureLoader = new THREE.TextureLoader();
  materialPlanetMap: THREE.MeshLambertMaterial;

  public rotationX = 0.0;
  public rotationY = 0.0;
  public rotationZ = 0.0;

  public cameraX = 250.0;
  public cameraY = 250.0;
  public cameraZ = 250.0;

  public cameraFar = 1100.0;
  public cameraNear = 1.0;
  public cameraFovr = 60.0;

  public translationY = 0.0;

  mPlanetSurfacePNG: string;
    rerender: boolean;
    meshPlanetMap: THREE.Mesh;
    geometryPlanetMap: THREE.SphereBufferGeometry;

  get PlanetSurfacePNG() {
    return this.mPlanetSurfacePNG;
  }

  @Input() set PlanetSurfacePNG(aPNGUrl: string) {
    this.mPlanetSurfacePNG = aPNGUrl;

    this.loader.load(this.PlanetSurfacePNG, PNG => {
      this.materialPlanetMap = new THREE.MeshLambertMaterial({ map: PNG });

      if (this.meshPlanetMap) this.scene.removeChild(this.meshPlanetMap);
      this.meshPlanetMap = new THREE.Mesh(this.geometryPlanetMap, this.materialPlanetMap);
      this.scene.addChild(this.meshPlanetMap);

      if(this.rerender) this.renderer.render()
    });
  }


  constructor() { }

  ngOnInit() {
  }

  ngAfterViewInit() {
    let PoleSkip = (Math.PI / 180) * 10;
    this.geometryPlanetMap = new THREE.SphereBufferGeometry(200, 40, 40, 0, Math.PI * 2, PoleSkip, Math.PI - 2 * PoleSkip);
    let geometryPole = new THREE.SphereBufferGeometry(199.5, 40, 40);
    let materialPole = new THREE.MeshLambertMaterial({ color: new Color(0xffffff) });
    let meshPole = new THREE.Mesh(geometryPole, materialPole);
    this.scene.addChild(meshPole);

    setTimeout(() => this.scene.InitChilds(), 1);
    setTimeout(() => { this.rerender = true; this.renderer.startRendering(); }, 2);

  }

}
