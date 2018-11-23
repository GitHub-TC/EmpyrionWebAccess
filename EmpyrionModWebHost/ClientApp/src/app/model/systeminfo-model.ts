
export class SystemInfoModel {
  online?: boolean;
  version?: string;
  activePlayers?: number;
  activePlayfields?: number;
  totalPlayfieldserver?: number;
  diskFreeSpace?: number;
  diskUsedSpace?: number;
  cpuTotalLoad?: number;
  ramAvailableMB?: number;
  ramTotalMB?: number;
  serverName?: string;
}
