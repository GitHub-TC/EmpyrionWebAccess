import { ActivePlayfieldModel } from "./activeplayfield-model";
import { PLAYER } from "./player-mock";

export const ACTIVEPLAYFIELDS: ActivePlayfieldModel[] = [
  { name: "Akua", players: PLAYER.slice(0, 3) },
  { name: "Philippos", players: PLAYER.slice(4, 5) },
];

