import { ItemStackModel } from "./itemstack-model";

export class BackpackModel {
  entityPlayerId: number;
  backpack: ItemStackModel[];
}

export const EmptyBackpack: BackpackModel = { entityPlayerId: 43, backpack: [] };
