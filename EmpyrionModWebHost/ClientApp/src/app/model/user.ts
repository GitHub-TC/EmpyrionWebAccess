export enum RoleEnum {
  ServerAdmin = "Server Admin",
  InGameAdmin = "In Game Admin",
  Moderator   = "Moderator",
  GameMaster  = "Game Master",
  VIP         = "VIP Player",
  Player      = "Player",
  None        = "No Access"
}

export class User {
    id: number;
    username: string;
    password: string;
    inGameSteamId?: string;
    role: RoleEnum;
    token?: string;
}
