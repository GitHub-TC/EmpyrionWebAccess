import { ItemStackModel } from "./itemstack-model";

export class BackpackModel {
  SteamId?: string;
  Toolbar?: ItemStackModel[];
  Bag?: ItemStackModel[];
}

export const EmptyBackpack: BackpackModel = { SteamId: "", Toolbar: [], Bag: [] };
