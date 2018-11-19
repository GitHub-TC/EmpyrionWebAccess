import { ItemStackModel } from "./itemstack-model";

export class BackpackModel {
  steamId?: string;
  backpack?: ItemStackModel[];
}

export const EmptyBackpack: BackpackModel = { steamId: "ST42", backpack: [] };
