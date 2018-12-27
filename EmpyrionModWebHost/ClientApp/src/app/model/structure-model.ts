import { PVector3 } from "./player-model";

  export class GlobalStructureInfo
  {
    id: number;
    playfield?: string;
    dockedShips?: number[];
    classNr?: number;
    cntLights?: number;
    cntTriangles?: number;
    cntBlocks?: number;
    cntDevices?: number;
    fuel?: number;
    powered?: boolean;
    rot?: PVector3;
    pos?: PVector3;
    lastVisitedUTC?: number;
    name?: string;
    factionId?: number;
    factionGroup?: number;
    FactionName: string;
    type?: number;
    TypeName: string;
    coreType?: number;
    CoreName: string;
    pilotId?: number;
}
