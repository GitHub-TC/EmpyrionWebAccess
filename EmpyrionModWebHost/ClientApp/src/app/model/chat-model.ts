export enum ChatType {
  Global = 3,
  Faction = 5,
  Private = 1,
};

export class ChatModel {
  Id?: number;
  Type?: number;
  Timestamp?: Date;
  FactionId?: number;
  FactionName?: string;
  PlayerSteamId?: string;
  PlayerName?: string;
  Message?: string;
}
