import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import * as jsyaml from 'js-yaml';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { SceneDirective } from '../three-js/objects/scene.directive';
import { FlatTreeControl } from '@angular/cdk/tree';
import { MatTreeFlattener, MatTreeFlatDataSource } from '@angular/material';
import * as THREE from 'three';

export class WarpRoute {
  startOrbit: string;
  start: THREE.Vector3;
  endOrbit: string;
  end: THREE.Vector3;
  isBidirectional: boolean;
  color: number;
}

export class SectorMap {
  Sectors: Sector[]
}

export class Sector {
  Name: string;
  Coordinates: number[];
  SectorMapType: string;
  Allow: string[];
  Deny: string[];
  Playfields: string[][];
}

/** Flat node with expandable and level information */
interface ExampleFlatNode {
  expandable: boolean;
  name: string;
  data: Sector | string[];
  level: number;
}

@Component({
  selector: 'app-galaxy-map',
  templateUrl: './galaxy-map.component.html',
  styleUrls: ['./galaxy-map.component.less']
})
export class GalaxyMapComponent implements OnInit {
  @ViewChild(WebGLRendererComponent) renderer;
  @ViewChild(SceneDirective) scene;

  public rotationX = 0.0;
  public rotationY = 0.0;
  public rotationZ = 0.0;

  public cameraX = 20.0;
  public cameraY = 50.0;
  public cameraZ = 50.0;

  public cameraFar = 1100.0;
  public cameraNear = 1.0;
  public cameraFovr = 60.0;

  public translationY = 0.0;

  public Sectors: SectorMap = { Sectors: [] };
  public WarpRoutes: WarpRoute[] = []
  error: any;

  private transformer = (node: Sector | string[], level: number) => {
    let nodeSector    = <Sector>node;
    let nodePlayfield = <string[]>node;
    let isSector = nodeSector.Playfields;
    return {
      expandable: isSector && nodeSector.Playfields.length > 1,
      name:       isSector ? nodeSector.Playfields[nodeSector.Playfields.length - 1][1] : nodePlayfield[1],
      data:       node,
      level:      level,
    };
  }

  treeControl = new FlatTreeControl<ExampleFlatNode>(
    node => node.level, node => node.expandable);

  treeFlattener = new MatTreeFlattener(
    this.transformer, node => node.level, node => node.expandable, node => (<Sector>node).Playfields);

  dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);

  constructor(
    public router: Router,
    private http: HttpClient) {
  }

  ngOnInit() {
    this.http.get('Playfield/Sectors', { responseType: 'text' })
      .pipe()
      .subscribe(
        F => {
          this.Sectors = jsyaml.load(F);
          this.Sectors.Sectors = this.Sectors.Sectors.map(S => { S.Name = S.Playfields[S.Playfields.length - 1][1]; return S; });
          this.Sectors.Sectors = this.Sectors.Sectors.sort((A, B) => A.Name.localeCompare(B.Name));
          this.dataSource.data = this.Sectors.Sectors;
          this.CalcWarpRoutes();
          setTimeout(() => this.scene.InitChilds(),        1);
          setTimeout(() => this.renderer.startRendering(), 2);
        },
        error => this.error = error // error path
    );
  }

  applyFilter(aEvent) {
    let filterValue = aEvent.target.value.trim().toLowerCase();
    this.dataSource.data = filterValue
      ? this.Sectors.Sectors.filter(S => S.Playfields.find(P => P[1].toLowerCase().indexOf(filterValue) >= 0))
      : this.Sectors.Sectors;
  }

  Goto(node) {
    if (!node.data.Coordinates) return;

    this.cameraX = node.data.Coordinates[0] + 5;
    this.cameraY = node.data.Coordinates[1] + 5;
    this.cameraZ = node.data.Coordinates[2] + 5;
  }

  CalcWarpRoutes(): any {
    for (var from = this.Sectors.Sectors.length - 1; from >= 0; from--) {
      if (this.Sectors.Sectors[from].SectorMapType.toLowerCase() != "none") {
        for (var to = this.Sectors.Sectors.length - 1; to >= 0; to--) {
          if (from != to) {
            let start = this.ToVectorX(this.Sectors.Sectors[from].Coordinates);
            let end = this.ToVectorX(this.Sectors.Sectors[to].Coordinates);

            let dist = start.distanceTo(end);
            let warp = dist <= 250;
            if (this.Sectors.Sectors[to].SectorMapType.toLowerCase() == "none") warp = false;
            if (this.Sectors.Sectors[from].Deny && this.Sectors.Sectors[from].Deny.find(N => N == this.Sectors.Sectors[to].Name)) warp = false;
            if (this.Sectors.Sectors[from].Allow && this.Sectors.Sectors[from].Allow.find(N => N == this.Sectors.Sectors[to].Name)) warp = true;

            if (warp) this.InsertWarp(this.Sectors.Sectors[from], this.Sectors.Sectors[to], dist);
          }
        }
      }
    }
  }

  InsertWarp(aStart: Sector, aEnd: Sector, aDistance: number): any {
    let Found = this.WarpRoutes.find(W => (W.startOrbit == aStart.Name && W.endOrbit == aEnd.Name) || (W.startOrbit == aEnd.Name && W.endOrbit == aStart.Name));
    if (Found) {
      Found.isBidirectional = true;
    }
    else this.WarpRoutes.push({
      start: this.ToVectorX(aStart.Coordinates),
      startOrbit: aStart.Name,
      end: this.ToVectorX(aEnd.Coordinates),
      endOrbit: aEnd.Name,
      isBidirectional: false,
      color: this.WarpColor(aDistance)
    });
  }

  WarpColor(aDistance: number): number {
    if (aDistance >  250) return 0x000000;
    if (aDistance >= 200) return 0xff0000;
    if (aDistance >= 150) return 0xff9900;
    return 0xffcc00
  }

  ToVectorX(aCoordinates: number[]): THREE.Vector3 {
    return new THREE.Vector3(aCoordinates[0], aCoordinates[1], aCoordinates[2]);
  }

  get OneWayRoutes() {
    return this.WarpRoutes.filter(W => !W.isBidirectional);
  }

  hasChild = (_: number, node: ExampleFlatNode) => node.expandable;
}
