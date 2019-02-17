import { Directive, forwardRef, Input } from '@angular/core';
import { AbstractObject3D } from '../abstract-object-3d';
import { AbstractModelLoader } from './abstract-model-loader';

import * as THREE from 'three';
import { FontService } from '../../services/font.service';

@Directive({
  selector: 'text-loader',
  providers: [{ provide: AbstractObject3D, useExisting: forwardRef(() => TextLoaderDirective) }]
})
export class TextLoaderDirective extends AbstractModelLoader {
  @Input() Text: string;
  @Input() Size: number;
  @Input() Height: number;
  @Input() Color: number;
  @Input() LookAt: boolean = true;

  constructor(private fonts: FontService) {
    super();
    this.CheckCamera();
  }

  CheckCamera() {
    setTimeout(() => this.CheckCamera(), 1000);
    if (this.LookAt &&
      this.renderer &&
      this.renderer.cameraComponents &&
      this.renderer.cameraComponents.length) this.getObject().lookAt(this.renderer.cameraComponents.first.camera.position);
  }

  protected async loadModelObject() {
    let _this = this;

    return new Promise<THREE.Object3D>((resolve, reject) => {
      _this.fonts.getFont("Arial_Regular").then(F => {
        let textGeo = new THREE.TextGeometry(_this.Text, {
          font: F,
          size: _this.Size,
          height: _this.Height,
          //curveSegments: curveSegments,
          //bevelThickness: bevelThickness,
          //bevelSize: bevelSize,
          //bevelEnabled: bevelEnabled,
          //material: 0,
          //extrudeMaterial: 1
        });

        resolve(new THREE.Mesh(textGeo, new THREE.MeshBasicMaterial({ color: _this.Color })));
      });
    });
  }
}
