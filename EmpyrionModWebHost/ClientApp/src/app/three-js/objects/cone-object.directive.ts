import { Directive, forwardRef, Input } from '@angular/core';

import * as THREE from 'three';
import { PlainObject3D } from './plain-object-3d';

@Directive({
  selector: 'cone-object-loader',
  providers: [{ provide: PlainObject3D, useExisting: forwardRef(() => ConeLoaderDirective) }]
})
export class ConeLoaderDirective extends PlainObject3D<THREE.Object3D> {
  @Input() Start: THREE.Vector3;
  @Input() End:   THREE.Vector3;
  @Input() Color: number = 0x000000;

  protected newObject3DInstance() {
    let start = this.Start.clone();
    let end   = this.End.clone();

    return new THREE.ArrowHelper(end.sub(start).normalize(), this.Start, this.End.distanceTo(this.Start) / 2, this.Color, 6, 2);
  }

  protected afterInit() {
  }
}
