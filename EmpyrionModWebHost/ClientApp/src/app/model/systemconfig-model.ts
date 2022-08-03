export class ProcessInformation {
  id?: number
  currentDirecrory?: string;
  arguments?: string;
  fileName?: string;
}

export class SystemConfig {
  processInformation?: ProcessInformation;
  startCMD?: string;
  welcomeMessage?: string;
  playerSteamInfoUrl?: string;
}
