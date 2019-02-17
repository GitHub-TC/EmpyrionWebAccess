import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import * as jsyaml from 'js-yaml';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { BehaviorSubject, Observable } from 'rxjs';
import { SceneDirective } from '../three-js/objects/scene.directive';

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

  public Sectors = [];
  error: any;

  constructor(
    public router: Router,
    private http: HttpClient) {
  }

  ngOnInit() {
    this.http.get('Playfield/Sectors', { responseType: 'text' })
      .pipe()
      .subscribe(
        F => {
          this.Sectors = jsyaml.load(F).Sectors;
          setTimeout(() => this.scene.InitChilds(),        1);
          setTimeout(() => this.renderer.startRendering(), 2);
        },
        error => this.error = error // error path
    );
  }

}
