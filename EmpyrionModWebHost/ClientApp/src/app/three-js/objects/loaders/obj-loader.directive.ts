import { Directive, forwardRef, Input } from '@angular/core';
import * as THREE from 'three';
import { AbstractObject3D } from '../abstract-object-3d';
import { AbstractModelLoader } from './abstract-model-loader';

import '../../js/EnableThreeJs';
import '../../js/LegacyJSONLoader';
//import 'three/examples/js/loaders/OBJLoader';
//import 'three/examples/js/loaders/MTLLoader';
import { PlainObject3D } from '../plain-object-3d';

/**
 * Directive for employing THREE.OBJLoader to load [Wavefront *.obj files][1].
 *
 * [1]: https://en.wikipedia.org/wiki/Wavefront_.obj_file
 */
@Directive({
  selector: 'three-obj-loader',
  providers: [
    { provide: AbstractObject3D, useExisting: forwardRef(() => ObjLoaderDirective) },
    { provide: PlainObject3D, useExisting: forwardRef(() => ObjLoaderDirective) }]
})
export class ObjLoaderDirective extends AbstractModelLoader {
  private loader: THREE.OBJLoader; 
  private mtlLoader: THREE.MTLLoader;

  @Input()
  material: string;

  @Input()
  texturePath: string;

  constructor() {
    super();
    let three = THREE;
    this.loader    = new three.OBJLoader();
    this.mtlLoader = new three.MTLLoader();
  }

  protected async loadModelObject() {
    // TODO: make it nicer
    if (this.material === undefined) {
      return new Promise<THREE.Object3D>((resolve, reject) => {
        this.loader.load(this.model, model => {
          resolve(model);
        },
          undefined,
          reject
        );
      });
    } else {
      return new Promise<THREE.Object3D>((resolve, reject) => {
        if (this.texturePath !== undefined) {
          this.mtlLoader.setTexturePath(this.texturePath);
        }
        this.mtlLoader.load(this.material, material => {
          material.preload();
          this.loader.setMaterials(material);
          this.loader.load(this.model, model => {
            resolve(model);
          },
            undefined,
            reject
          );
        });
      });
    }
  }
}
