import { AfterViewInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import * as THREE from 'three';
import { PlainObject3D } from './plain-object-3d';

export abstract class AbstractObject3D<T extends THREE.Object3D> extends PlainObject3D<T> implements AfterViewInit, OnChanges {
  /**
   * Rotation in Euler angles (radians) with order X, Y, Z.
   */
  @Input() rotateX: number;

  /**
   * Rotation in Euler angles (radians) with order X, Y, Z.
   */
  @Input() rotateY: number;

  /**
   * Rotation in Euler angles (radians) with order X, Y, Z.
   */
  @Input() rotateZ: number;

  @Input() translateX: number;
  @Input() translateY: number;
  @Input() translateZ: number;

  @Input() lateInit: boolean = false;

  protected rerender() {
  }

  public ngOnChanges(changes: SimpleChanges) {
    if (!this.object) {
      return;
    }

    let mustRerender = false;

    if (['rotateX', 'rotateY', 'rotateZ'].some(propName => propName in changes)) {
      this.applyRotation();
      mustRerender = true;
    }
    if (['translateX', 'translateY', 'translateZ'].some(propName => propName in changes)) {
      this.applyTranslation();
      mustRerender = true;
    }

    if (mustRerender) {
      this.rerender();
    }
  }

  public ngAfterViewInit(): void {
    console.log('AbstractObject3D.ngAfterViewInit');
    this.object = this.newObject3DInstance();

    this.applyTranslation();
    this.applyRotation();

    if (!this.lateInit) this.InitChilds();
  }

  protected applyRotation(): void {
    const angles = [
      this.rotateX,
      this.rotateY,
      this.rotateZ
    ].map(angle => angle || 0);

    this.object.rotation.set(
      this.rotateX || 0,
      this.rotateY || 0,
      this.rotateZ || 0,
      'XYZ'
    );
  }

  private applyTranslation(): void {
    this.object.position.set(
      this.translateX || 0,
      this.translateY || 0,
      this.translateZ || 0
    );
  }

}
