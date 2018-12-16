export class PVector3 {
  x: number;
  y: number;
  z: number;
}

export class PositionModel {
  description?: string;
  playfield?: string;
  entityId?: number;
  pos?: PVector3;
  rot?: PVector3;
}

export class PlayerModel {
  Online?: boolean;
  ClientId?: number;
  Radiation?: number;
  RadiationMax?: number;
  BodyTemp?: number;
  BodyTempMax?: number;
  Kills?: number;
  Died?: number;
  Credits?: number;
  FoodMax?: number;
  Exp?: number;
  Upgrade?: number;
  BpRemainingTime?: number;
  BpInFactory?: string;
  Ping?: number;
  Permission?: number;
  Food?: number;
  Stamina?: number;
  EntityId?: number;
  SteamId?: string;
  SteamOwnerId?: string;
  PlayerName?: string;
  Playfield?: string;
  StartPlayfield?: string;
  Pos?: PVector3;
  StaminaMax?: number;
  Rot?: PVector3;
  FactionGroup?: number;
  FactionId?: number;
  FactionRole?: number;
  Health?: number;
  HealthMax?: number;
  Oxygen?: number;
  OxygenMax?: number;
  Origin?: number;
  PosX?: number;
  PosY?: number;
  PosZ?: number;
  RotX?: number;
  RotY?: number;
  RotZ?: number;
  Note?: string;
  LastOnline?: string | Date;
  OnlineTime?: string;
}


export class PlayerInfoSet {
  entityId: number;
  sendLastNLogs?: number;
  factionRole?: number;
  factionId?: number;
  factionGroup?: number;
  origin?: number;
  upgradePoints?: number;
  experiencePoints?: number;
  bodyTempMax?: number;
  bodyTemp?: number;
  bpRemainingTime?: number;
  radiationMax?: number;
  oxygenMax?: number;
  oxygen?: number;
  foodMax?: number;
  food?: number;
  staminaMax?: number;
  stamina?: number;
  healthMax?: number;
  health?: number;
  startPlayfield: string;
  radiation?: number;
}
