import { Directive, Input, forwardRef, HostListener, SimpleChanges, Output, EventEmitter } from '@angular/core';
import { AbstractCamera } from './abstract-camera';
import * as THREE from 'three';
import { WebGLRendererComponent } from '../renderer/webgl-renderer.component';

export class MouseIntersection {
  public event: MouseEvent;
  public targets: THREE.Intersection[];
}

@Directive({
  selector: 'three-perspective-camera',
  providers: [{ provide: AbstractCamera, useExisting: forwardRef(() => PerspectiveCameraDirective) }]
})
export class PerspectiveCameraDirective extends AbstractCamera<THREE.PerspectiveCamera> {
  private _renderer: WebGLRendererComponent;
  @Output() cameraTargets = new EventEmitter<MouseIntersection>();

  selectedObject = null;
  raycaster = new THREE.Raycaster();

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
    super();
    console.log('PerspectiveCameraDirective.constructor');
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
    window.addEventListener("mousemove", E => this.onDocumentMouseEvent(E), false);
    window.addEventListener("click",     E => this.onDocumentMouseEvent(E), false);
  }

  onDocumentMouseEvent(event: MouseEvent) {
    if (!this._renderer.isViewInitialized) return;

    let bounds = this._renderer.canvas.getBoundingClientRect()
    let mouse = new THREE.Vector2();
    mouse.x =   ((event.clientX - bounds.left) / this._renderer.canvas.clientWidth) * 2 - 1;
    mouse.y = - ((event.clientY - bounds.top) / this._renderer.canvas.clientHeight) * 2 + 1;
    this.raycaster.setFromCamera(mouse, this.camera);
    var intersects = this.raycaster.intersectObjects(this._renderer.scene.children, true);
    if (intersects.length > 0) {
      event.preventDefault();
      //console.log("Objects:" + intersects.length);
      this.cameraTargets.emit({ event: event, targets: intersects });
    }
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

    this.renderer.render();
  }

  @Input()
  public set renderer(newRenderer: WebGLRendererComponent) {
    this._renderer = newRenderer;
    this._renderer.render();
  }

  public get renderer() {
    return this._renderer;
  }

  public updateAspectRatio(aspect: number) {
    console.log('PerspectiveCameraDirective.updateAspectRatio: ' + aspect);
    this.camera.aspect = aspect;
    this.camera.updateProjectionMatrix();
  }


}
