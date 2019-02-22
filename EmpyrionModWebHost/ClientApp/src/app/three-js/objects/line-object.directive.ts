import { Directive, forwardRef, Input } from '@angular/core';

import * as THREE from 'three';
import { PlainObject3D } from './plain-object-3d';

@Directive({
  selector: 'line-object-loader',
  providers: [{ provide: PlainObject3D, useExisting: forwardRef(() => LineLoaderDirective) }]
})
export class LineLoaderDirective extends PlainObject3D<THREE.Object3D> {
  @Input() Start: THREE.Vector3;
  @Input() End: THREE.Vector3;
  @Input() Color: number = 0x000000;

  protected newObject3DInstance() {
    var geometry = new THREE.Geometry();
    geometry.vertices.push(this.Start);
    geometry.vertices.push(this.End);

    return new THREE.Line(geometry, new THREE.LineBasicMaterial({ color: this.Color }));
  }

  protected afterInit() {
  }
}
