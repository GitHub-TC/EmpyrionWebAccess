export enum UserRole {
  ServerAdmin = 0,
  InGameAdmin = 1,
  Moderator   = 2,
  GameMaster  = 3,
  VIP         = 4,
  Player      = 5,
  None        = 6
}

export class User {
    id: number;
    username: string;
    password: string;
    inGameSteamId?: string;
    role: UserRole;
    token?: string;
}
