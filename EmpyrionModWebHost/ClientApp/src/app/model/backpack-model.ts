import { ItemStackModel } from "./itemstack-model";

export class BackpackModel {
  SteamId?: string;
  Timestamp?: Date;
  Toolbar?: ItemStackModel[];
  Bag?: ItemStackModel[];
}

export const EmptyBackpack: BackpackModel = { SteamId: "", Toolbar: [], Bag: [] };

export interface BackpackODataModel {
  id?: string;
  timestamp?: Date;
  toolbarContent?: string;
  bagContent?: string;
}

