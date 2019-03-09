import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { SceneDirective } from '../three-js/objects/scene.directive';
import * as THREE from 'three';
import { Color, Object3D } from 'three';
import { GlobalStructureInfo } from '../model/structure-model';
import { MouseIntersection } from '../three-js/cameras/perspective-camera.directive';
import { Observable, BehaviorSubject } from 'rxjs';
import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';

@Component({
  selector: 'app-playfield-spaceview3d',
  templateUrl: './playfield-spaceview3d.component.html',
  styleUrls: ['./playfield-spaceview3d.component.less']
})
export class PlayfieldSpaceview3dComponent implements OnInit {
  @ViewChild(WebGLRendererComponent) renderer: WebGLRendererComponent;
  @ViewChild(SceneDirective) scene: SceneDirective;

  ToolTipText: string;
  ToolTipX: string;
  ToolTipY: string;

  loader: THREE.ObjectLoader = new THREE.ObjectLoader();
  mStructures: GlobalStructureInfo[];
  mPlayers: PlayerModel[];

  get Structures() { return this.mStructures; }
  @Input() set Structures(aStructures: GlobalStructureInfo[]) { this.mStructures = aStructures; this.rerenderStructures = true; this.RenderStructures(); }

  get Players() { return this.mPlayers; }
  @Input() set Players(aPlayers: PlayerModel[]) { this.mPlayers = aPlayers; this.rerenderPlayers = true; this.RenderPlayers(); }

  public rotationX = 0.0;
  public rotationY = 0.0;
  public rotationZ = 0.0;

  public cameraX = 250.0;
  public cameraY = 250.0;
  public cameraZ = 250.0;

  public cameraFar = 10000.0;
  public cameraNear = 1.0;
  public cameraFovr = 60.0;

  rerenderPlayers: boolean;
  rerenderStructures: boolean;
  objBASpace:  THREE.Object3D;
  objCV:       THREE.Object3D;
  objCVSmall:  THREE.Object3D;
  objCVMedium: THREE.Object3D;
  objHV:       THREE.Object3D;
  objSV:       THREE.Object3D;
  objPlayer:   THREE.Object3D;

  lastCurrentStructure: { structure: GlobalStructureInfo, object: THREE.Object3D };
  lastCurrentPlayer:    { player: PlayerModel,            object: THREE.Object3D };

  PI2: number = Math.PI / 180;
  StructureObjectView: Array<{ id: number, object: THREE.Object3D }> = [];
  PlayerObjectView: Array<{ steamId: string, object: THREE.Object3D }> = [];

  constructor(
    private mStructureService: StructureService,
    private mPlayerService: PlayerService,
  ) {
    this.loader.load("assets/Model/BASpace.json", O => {
      this.objBASpace = O; this.RenderStructures();
      this.objBASpace.scale.x = 10;
      this.objBASpace.scale.y = 10;
      this.objBASpace.scale.z = 10;
    });
    this.loader.load("assets/Model/CV.json", O => {
      this.objCV = O; this.RenderStructures();
      this.objCV.scale.x = 6;
      this.objCV.scale.y = 6;
      this.objCV.scale.z = 6;
    });
    this.loader.load("assets/Model/CVMedium.json", O => {
      this.objCVMedium = O; this.RenderStructures();
      this.objCVMedium.scale.x = 2;
      this.objCVMedium.scale.y = 2;
      this.objCVMedium.scale.z = 2;
    });
    this.loader.load("assets/Model/CVSmall.json", O => {
      this.objCVSmall = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/HV.json", O => {
      this.objHV = O; this.RenderStructures();
    });
    this.loader.load("assets/Model/SV.json", O => {
      this.objSV = O; this.RenderStructures();
      this.objSV.scale.x = 10;
      this.objSV.scale.y = 10;
      this.objSV.scale.z = 10;
    });
    this.loader.load("assets/Model/Player.json", O => {
      this.objPlayer = O; this.RenderPlayers();
      this.objPlayer.scale.x = 4;
      this.objPlayer.scale.y = 4;
      this.objPlayer.scale.z = 4;
    });

    mStructureService.GetCurrentStructure().subscribe(S => this.HighlightCurrentStructure(S));
    mPlayerService.GetCurrentPlayer().subscribe(P => this.HighlightCurrentPlayer(P));
  }

  ngOnInit() {
  }

  HighlightCurrentStructure(structure: GlobalStructureInfo) {
    if (this.lastCurrentStructure) {
      this.ChangeColor(this.lastCurrentStructure.object, M => {
        M.material = M.userData.saveMaterial;
        return true;
      });
    }
    this.lastCurrentStructure = null;

    if (!structure || !this.StructureObjectView) return;

    let Found = this.StructureObjectView.find(S => S.id == structure.id);
    if (!Found) return;

    this.ChangeColor(Found.object, M => {
      M.userData.saveMaterial = M.material;
      M.material = new THREE.MeshBasicMaterial({ color: new Color(0x007f00) });
      return true;
    });

    this.lastCurrentStructure = { structure: structure, object: Found.object };
  }

  HighlightCurrentPlayer(Player: PlayerModel) {
    if (this.lastCurrentPlayer) {
      this.ChangeColor(this.lastCurrentPlayer.object, M => {
        M.material = M.userData.saveMaterial;
        return true;
      });
    }
    this.lastCurrentPlayer = null;

    if (!Player || !this.PlayerObjectView) return;

    let Found = this.PlayerObjectView.find(S => S.steamId == Player.SteamId);
    if (!Found) return;

    this.ChangeColor(Found.object, M => {
      M.userData.saveMaterial = M.material;
      M.material = new THREE.MeshBasicMaterial({ color: new Color(0x007f00) });
      return true;
    });

    this.lastCurrentPlayer = { player: Player, object: Found.object };
  }

  ChangeColor(aObject: Object3D, aChange: (M: THREE.Mesh) => boolean): any {
    let S = aObject as THREE.Scene;
    if (S) {
      let changed = false;
      S.children.map(C => {
        let MC = C as THREE.Mesh;
        if (MC && aChange(MC)) changed = true;
      });
      if (changed) this.renderer.render();
    }
  }

  RenderStructures(): any {
    if (!this.rerenderStructures || !this.objPlayer || !this.objBASpace || !this.objCVSmall || !this.objHV || !this.objSV || !this.Structures) return;

    this.rerenderStructures = false;
    this.StructureObjectView.map(O => this.scene.removeChild(O.object));
    this.StructureObjectView = [];

    this.Structures.map(S => {
      let insertStructure : THREE.Object3D;
      switch (S.TypeName) {
        case "BA": insertStructure = this.objBASpace; break;
        case "CV": insertStructure =
            S.classNr > 10 ? this.objCV :
            S.classNr >  5 ? this.objCVMedium
                           : this.objCVSmall; break;
        case "HV": insertStructure = this.objHV; break;
        case "SV": insertStructure = this.objSV; break;
      }

      if (insertStructure) {

        insertStructure = insertStructure.clone();
        insertStructure.position.x = S.pos.x / 5;
        insertStructure.position.y = S.pos.y / 5;
        insertStructure.position.z = S.pos.z / 5;

        insertStructure.userData.tooltipText = (S.FactionName ? "(" + S.FactionName + ") " : "") + S.name;

        let ColorMaterial = new THREE.MeshBasicMaterial({ color: new Color(this.StructurColor(S)).multiplyScalar(0.7) });

        this.ChangeColor(insertStructure, M => {
          M.userData.saveMaterial = M.material;
          M.material = ColorMaterial;
          return true;
        });

        this.StructureObjectView.push({ id: S.id, object: insertStructure });

        this.scene.addChild(insertStructure);
      }
    });

    this.HighlightCurrentStructure(this.mStructureService.CurrentStructure);

    this.renderer.render();
  }

  StructurColor(aStructure: GlobalStructureInfo) {
    if (aStructure.factionGroup == 5 || (aStructure.CoreName && aStructure.CoreName.includes("Admin"))) return "purple";

    switch (aStructure.factionGroup) {
      case 0: return "magenta"; // Faction
      case 1: return "aqua"; // Privat
      case 2: return "tomato"; // Zirax
      case 3: return "tomato"; // Predator
      case 4: return "tomato"; // Prey
      case 5: return "purple"; // Admin;
      case 6: return "green";  // Talons
      case 7: return "blue";   // Polaris
      case 8: return "red";    // Aliens
      case 11: return "white"; // "Unknown";
      case 10: return "gold"; // Public
      case 12: return "white"; // "None";
      case 255: return "lightblue";   // No Core
      default: return "white";
    }
  }


  RenderPlayers(): any {
    if (!this.rerenderPlayers || !this.objPlayer || !this.Players) return;

    this.rerenderPlayers = false;
    this.PlayerObjectView.map(O => this.scene.removeChild(O.object));
    this.PlayerObjectView = [];

    this.Players.map(P => {
      let insertPlayer: THREE.Object3D = P.Online ? this.objPlayer : this.objPlayer;

      if (insertPlayer) {

        insertPlayer = insertPlayer.clone();
        insertPlayer.position.x = P.Pos.x / 5;
        insertPlayer.position.y = P.Pos.y / 5;
        insertPlayer.position.z = P.Pos.z / 5;

        insertPlayer.userData.tooltipText = P.PlayerName;

        this.PlayerObjectView.push({ steamId: P.SteamId, object: insertPlayer });

        this.scene.addChild(insertPlayer);
      }
    });

    this.HighlightCurrentPlayer(this.mPlayerService.CurrentPlayer);

    this.renderer.render();
  }

  selectedTargets(selected: MouseIntersection) {
    this.ToolTipText = null;
    let Found = selected.targets[0].object;
    while (Found && !Found.userData.tooltipText) {
      Found = Found.parent;
    }
    if (!Found) return;

    if (selected.event.type == "click") {
      let FoundStructure = this.StructureObjectView.find(S => S.object == Found);
      if (FoundStructure) this.mStructureService.CurrentStructure = this.Structures.find(S => S.id == FoundStructure.id);

      let FoundPlayer = this.PlayerObjectView.find(S => S.object == Found);
      if (FoundPlayer) this.mPlayerService.CurrentPlayer = this.Players.find(S => S.SteamId == FoundPlayer.steamId);
    }

    let canvasBounds = this.renderer.canvas.getBoundingClientRect();

    this.ToolTipText = Found.userData.tooltipText;
    this.ToolTipX = (selected.event.clientX - canvasBounds.left) + "px";
    this.ToolTipY = (selected.event.clientY - canvasBounds.top)  + "px";
  }

  ngAfterViewInit() {
    setTimeout(() => this.scene.InitChilds(), 1);
    setTimeout(() => {
      this.rerenderStructures = true;
      this.rerenderPlayers = true;
      this.renderer.startRendering();
      this.renderer.scene.background = new Color("lightgray");
    }, 2);
  }

}

