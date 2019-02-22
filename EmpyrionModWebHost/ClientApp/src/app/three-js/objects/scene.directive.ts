import { Directive, AfterViewInit, Input, forwardRef } from '@angular/core';
import * as THREE from 'three';
import { AbstractObject3D } from './abstract-object-3d';
import { PlainObject3D } from './plain-object-3d';

@Directive({
  selector: 'three-scene',
  providers: [
    { provide: AbstractObject3D, useExisting: forwardRef(() => SceneDirective) },
    { provide: PlainObject3D, useExisting: forwardRef(() => SceneDirective) }
  ]
})
export class SceneDirective extends AbstractObject3D<THREE.Scene> {

  constructor() {
    console.log('SceneDirective.constructor');
    super();
  }

  protected afterInit(): void {
    console.log('SceneDirective.afterInit');
  }

  protected newObject3DInstance(): THREE.Scene {
    console.log('SceneDirective.newObject3DInstance');
    return new THREE.Scene();
  }

}
