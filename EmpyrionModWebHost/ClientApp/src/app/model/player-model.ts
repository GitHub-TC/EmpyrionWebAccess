export class PVector3 {
  x: number;
  y: number;
  z: number;
}

export class PositionModel {
  playfield?: string;
  pos?: PVector3;
  rot?: PVector3;
}

export class PlayerModel {
  online?: boolean;
  clientId?: number;
  radiation?: number;
  radiationMax?: number;
  bodyTemp?: number;
  bodyTempMax?: number;
  kills?: number;
  died?: number;
  credits?: number;
  foodMax?: number;
  exp?: number;
  upgrade?: number;
  bpRemainingTime?: number;
  bpInFactory?: string;
  ping?: number;
  permission?: number;
  food?: number;
  stamina?: number;
  entityId?: number;
  steamId?: string;
  steamOwnerId?: string;
  playerName?: string;
  playfield?: string;
  startPlayfield?: string;
  pos?: PVector3;
  staminaMax?: number;
  rot?: PVector3;
  factionGroup?: number;
  factionId?: number;
  factionRole?: number;
  health?: number;
  healthMax?: number;
  oxygen?: number;
  oxygenMax?: number;
  origin?: number;
}
