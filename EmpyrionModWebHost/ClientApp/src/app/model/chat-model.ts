export enum ChatType {
  Private = 1,
  Global  = 3,
  Faction = 5,
};

export class ChatModel {
  Id?: number;
  Type?: ChatType;
  Timestamp?: Date;
  FactionId?: number;
  FactionName?: string;
  PlayerSteamId?: string;
  PlayerName?: string;
  Message?: string;
}
