import { ViewChild, Input } from '@angular/core';
import { WebGLRendererComponent } from '../three-js/renderer/webgl-renderer.component';
import { SceneDirective } from '../three-js/objects/scene.directive';
import * as THREE from 'three';
import { Color, Object3D } from 'three';
import { GlobalStructureInfo } from '../model/structure-model';
import { MouseIntersection } from '../three-js/cameras/perspective-camera.directive';
import { StructureService } from '../services/structure.service';
import { PlayerService } from '../services/player.service';
import { PlayerModel } from '../model/player-model';

export class PlayfieldView3D {
  @ViewChild(WebGLRendererComponent) renderer: WebGLRendererComponent;
  @ViewChild(SceneDirective) scene: SceneDirective;

  public ToolTipText: string;
  public ToolTipX: string;
  public ToolTipY: string;

  public loader: THREE.ObjectLoader = new THREE.ObjectLoader();
  public mStructures: GlobalStructureInfo[];
  public mPlayers: PlayerModel[];

  get Structures() { return this.mStructures; }
  @Input() set Structures(aStructures: GlobalStructureInfo[]) {
    this.mStructures = aStructures;
    this.rerenderStructures = true;
    this.RenderStructures();
  }

  get Players() { return this.mPlayers; }
  @Input() set Players(aPlayers: PlayerModel[]) {
    this.mPlayers = aPlayers;
    this.rerenderPlayers = true;
    this.RenderPlayers();
  }

  public rotationX = 0.0;
  public rotationY = 0.0;
  public rotationZ = 0.0;

  public cameraX = 250.0;
  public cameraY = 250.0;
  public cameraZ = 250.0;

  public cameraFar = 10000.0;
  public cameraNear = 1.0;
  public cameraFovr = 60.0;

  public rerenderPlayers: boolean;
  public rerenderStructures: boolean;
  public objBA:  THREE.Object3D;
  public objCV:       THREE.Object3D;
  public objCVSmall:  THREE.Object3D;
  public objCVMedium: THREE.Object3D;
  public objHV:       THREE.Object3D;
  public objSV:       THREE.Object3D;
  public objPlayer:   THREE.Object3D;
  public objAsteroid: THREE.Object3D;

  public lastCurrentStructure: { structure: GlobalStructureInfo, object: THREE.Object3D };
  public lastCurrentPlayer:    { player: PlayerModel,            object: THREE.Object3D };

  public PI2:   number = Math.PI / 2;
  public PI360: number = Math.PI / 180;
  public StructureObjectView: Array<{ id: number, object: THREE.Object3D }> = [];
  public PlayerObjectView: Array<{ steamId: string, object: THREE.Object3D }> = [];

  constructor(
    public mStructureService: StructureService,
    public mPlayerService: PlayerService,
  ) {

    mStructureService.GetCurrentStructure().subscribe(S => this.HighlightCurrentStructure(S));
    mPlayerService.GetCurrentPlayer().subscribe(P => this.HighlightCurrentPlayer(P));
  }

  public HighlightCurrentStructure(structure: GlobalStructureInfo) {
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

  public HighlightCurrentPlayer(Player: PlayerModel) {
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
      M.material = new THREE.MeshBasicMaterial({ color: new Color("yellow") });
      return true;
    });

    this.lastCurrentPlayer = { player: Player, object: Found.object };
  }

  public ChangeColor(aObject: Object3D, aChange: (M: THREE.Mesh) => boolean): any {
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

  public RenderStructures(): any {
    if (!this.rerenderStructures || !this.objAsteroid || !this.objBA || !this.objCVSmall || !this.objHV || !this.objSV || !this.Structures) return;

    //this.mStructures = [
    //  { name: "A", id: 1, pos: {x: 0, y:0, z:0}, TypeName:"BA" },
    //  { name: "B", id: 1, pos: { x: 0, y: 0, z: 500 }, TypeName: "BA" },
    //  { name: "C", id: 1, pos: { x: 0, y: 0, z: -1000 }, TypeName: "BA" },
    //  { name: "A1", id: 1, pos: { x: 2000, y: 0, z: 0 }, TypeName: "BA" },
    //  { name: "B1", id: 1, pos: { x: 2000, y: 0, z: 500 }, TypeName: "BA" },
    //  { name: "C1", id: 1, pos: { x: 2000, y: 0, z: -1000 }, TypeName: "BA" },
    //  { name: "A2", id: 1, pos: { x: 4000, y: 0, z: 0 }, TypeName: "BA" },
    //  { name: "B2", id: 1, pos: { x: 4000, y: 0, z: 500 }, TypeName: "BA" },
    //  { name: "C2", id: 1, pos: { x: 4000, y: 0, z: -1000 }, TypeName: "BA" },
    //  { name: "A3", id: 1, pos: { x: 6000, y: 0, z: 0 }, TypeName: "BA" },
    //  { name: "B3", id: 1, pos: { x: 6000, y: 0, z: 500 }, TypeName: "BA" },
    //  { name: "C3", id: 1, pos: { x: 6000, y: 0, z: -1000 }, TypeName: "BA" },
    //];

    this.rerenderStructures = false;
    this.StructureObjectView.map(O => this.scene.removeChild(O.object));
    this.StructureObjectView = [];

    this.Structures.map(S => {
      let insertStructure : THREE.Object3D;
      switch (S.TypeName) {
        case "BA": insertStructure = this.objBA; break;
        case "CV": insertStructure =
            S.classNr > 10 ? this.objCV :
            S.classNr >  5 ? this.objCVMedium
                           : this.objCVSmall; break;
        case "HV": insertStructure = this.objHV; break;
        case "SV": insertStructure = this.objSV; break;
        case "AstVoxel": insertStructure = this.objAsteroid; break;
      }

      if (insertStructure) {

        insertStructure = insertStructure.clone();
        this.SetStructurePos(insertStructure, S);

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

    console.log("Structures add:" + this.StructureObjectView.length);

    this.renderer.render();
  }

  SetStructurePos(insertStructure: Object3D, S: GlobalStructureInfo): any {
  }

  public StructurColor(aStructure: GlobalStructureInfo) {
    if (aStructure.factionGroup == 5 || (aStructure.CoreName && aStructure.CoreName.includes("Admin"))) return "purple";
    if (aStructure.TypeName == "AstVoxel") return "darkblue";

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


  public RenderPlayers(): any {
    if (!this.rerenderPlayers || !this.objPlayer || !this.Players) return;

    this.rerenderPlayers = false;
    this.PlayerObjectView.map(O => this.scene.removeChild(O.object));
    this.PlayerObjectView = [];

    this.Players.map(P => {
      let insertPlayer: THREE.Object3D = P.Online ? this.objPlayer : this.objPlayer;

      if (insertPlayer) {

        insertPlayer = insertPlayer.clone();
        this.SetPlayerPos(insertPlayer, P);

        insertPlayer.userData.tooltipText = P.PlayerName;

        let ColorMaterial = new THREE.MeshBasicMaterial({ color: new Color(P.Online ? "green" : "cornflowerblue").multiplyScalar(0.7) });

        this.ChangeColor(insertPlayer, M => {
          M.userData.saveMaterial = M.material;
          M.material = ColorMaterial;
          return true;
        });

        this.PlayerObjectView.push({ steamId: P.SteamId, object: insertPlayer });

        this.scene.addChild(insertPlayer);
      }
    });

    this.HighlightCurrentPlayer(this.mPlayerService.CurrentPlayer);

    console.log("Player add:" + this.PlayerObjectView.length);

    this.renderer.render();
  }

  SetPlayerPos(insertPlayer: Object3D, P: PlayerModel): any {
  }

  public selectedTargets(selected: MouseIntersection) {
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
      this.rerenderPlayers    = true;
      this.renderer.startRendering();
    }, 2);
    setTimeout(() => this.renderer.scene.background = new Color("lightgray"), 3);
  }

}

