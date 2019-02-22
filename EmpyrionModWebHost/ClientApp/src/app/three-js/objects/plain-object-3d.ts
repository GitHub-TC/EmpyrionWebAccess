import {
  AfterViewInit,
  ContentChildren,
  Input,
  OnChanges,
  QueryList,
  SimpleChanges
} from '@angular/core';
import * as THREE from 'three';

export abstract class PlainObject3D<T extends THREE.Object3D> implements AfterViewInit, OnChanges {

  @ContentChildren(PlainObject3D, { descendants: false }) childNodes: QueryList<PlainObject3D<THREE.Object3D>>;

  @Input() lateInit: boolean = false;

  protected object: T;

  protected rerender() {
  }

  public ngOnChanges(changes: SimpleChanges) {
    if (!this.object) {
      return;
    }

    this.rerender();
  }

  public ngAfterViewInit(): void {
    console.log('AbstractObject3D.ngAfterViewInit');
    this.object = this.newObject3DInstance();

    if (!this.lateInit) this.InitChilds();
  }

  public InitChilds() {
    if (this.childNodes !== undefined && this.childNodes.length > 1) {
      this.childNodes.filter(i => i !== this && i.getObject() !== undefined).forEach(i => {
        // console.log("Add child for " + this.constructor.name);
        // console.log(i);
        this.addChild(i.getObject());
      });
    } else {
      // console.log("No child Object3D for: " + this.constructor.name);
    }

    this.afterInit();
  }

  protected addChild(object: THREE.Object3D): void {
    this.object.add(object);
  }

  protected removeChild(object: THREE.Object3D): void {
    this.object.remove(object);
  }

  public getObject(): T {
    return this.object;
  }

  protected abstract newObject3DInstance(): T;

  protected abstract afterInit(): void;

}
