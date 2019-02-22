import { Directive, forwardRef } from '@angular/core';


import * as THREE from 'three';
import '../../js/EnableThreeJs';
import 'three/examples/js/loaders/ColladaLoader';
import { AbstractModelLoader } from './abstract-model-loader';
import { AbstractObject3D } from '../abstract-object-3d';
import { PlainObject3D } from '../plain-object-3d';

@Directive({
  selector: 'three-collada-loader',
  providers: [
    { provide: AbstractObject3D, useExisting: forwardRef(() => ColladaLoaderDirective) },
    { provide: PlainObject3D, useExisting: forwardRef(() => ColladaLoaderDirective) }
  ]
})
export class ColladaLoaderDirective extends AbstractModelLoader {
  private loader: THREE.ColladaLoader;

  constructor() {
    super();
    let three = THREE;
    this.loader = new three.ColladaLoader();
  }

  protected async loadModelObject() {
    return new Promise<THREE.Object3D>((resolve, reject) => {
      this.loader.load(this.model, model => {
          resolve(model.scene);
        },
        undefined,
        reject
      );
    });
  }
}
