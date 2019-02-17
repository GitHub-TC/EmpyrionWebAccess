import { Directive, Input, forwardRef, HostListener, SimpleChanges } from '@angular/core';
import { AbstractCamera } from './abstract-camera';
import * as THREE from 'three';

@Directive({
  selector: 'three-perspective-camera',
  providers: [{ provide: AbstractCamera, useExisting: forwardRef(() => PerspectiveCameraDirective) }]
})
export class PerspectiveCameraDirective extends AbstractCamera<THREE.PerspectiveCamera> {

  // @Input() cameraTarget: THREE.Object3D;

  _fov: number;
  _near: number;
  _far: number;

  _positionX: number;
  _positionY: number;
  _positionZ: number;

  get fov()  { return this._fov; }
  get near() { return this._near; }
  get far()  { return this._far; }

  @Input() set fov (n: number) { this._fov = n;  this.UpdateCamera(); }
  @Input() set near(n: number) { this._near = n; this.UpdateCamera(); }
  @Input() set far (n: number) { this._far = n;  this.UpdateCamera(); }

  get positionX() { return this._positionX; }
  get positionY() { return this._positionY; }
  get positionZ() { return this._positionZ; }

  @Input() set positionX(n: number) { this._positionX = n; this.UpdateCamera(); }
  @Input() set positionY(n: number) { this._positionY = n; this.UpdateCamera(); }
  @Input() set positionZ(n: number) { this._positionZ = n; this.UpdateCamera(); }

  constructor() {
    console.log('PerspectiveCameraDirective.constructor');
    super();
  }

  protected afterInit(): void {
    console.log('PerspectiveCameraDirective.afterInit');
    // let aspectRatio = undefined; // Updated later
    this.camera = new THREE.PerspectiveCamera(
      this.fov,
      undefined,
      this.near,
      this.far
    );

    // Set position and look at
    this.UpdateCamera();
  }

  public get rotation() {
    return this.camera.rotation;
  }

  private UpdateCamera() {
    if (!this.camera) return;

    this.camera.far  = this.far;
    this.camera.near = this.near;
    this.camera.fov  = this.fov;

    this.camera.position.x = this.positionX;
    this.camera.position.y = this.positionY;
    this.camera.position.z = this.positionZ;
    this.camera.updateProjectionMatrix();
  }

  public updateAspectRatio(aspect: number) {
    console.log('PerspectiveCameraDirective.updateAspectRatio: ' + aspect);
    this.camera.aspect = aspect;
    this.camera.updateProjectionMatrix();
  }


}
