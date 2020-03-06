import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { AuthHubConnectionBuilder } from '../_helpers';
import { HubConnection } from '@aspnet/signalr';

export class ModData {
  name: string;
  possibleNames: string[];
  active: boolean;
    configurationType: string;
}

@Component({
  selector: 'app-server-mod-manager',
  templateUrl: './server-mod-manager.component.html',
  styleUrls: ['./server-mod-manager.component.less']
})
export class ServerModManagerComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  Mods: ModData[] = [];
  _SelectedModConfig: ModData;
  get SelectedModConfig() : ModData { return this._SelectedModConfig; }
  ShortModName: string;
  selectedMatTabIndex: number = 0;
  error: any;
  IsModLoaderInstalled: string;
  ModsStarted: boolean;
  hubConnection: HubConnection;

  constructor(
    private http: HttpClient,
    private builder: AuthHubConnectionBuilder
  ) {
    this.hubConnection = builder.withAuthUrl('/hubs/modinfo').build();

    // message coming from the server
    this.hubConnection.on("ModHostRunning", B => this.ModsStarted = JSON.parse(B));

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  ngOnInit() {
    this.ModLoaderInstalled();
  }

  ModLoaderInstalled() {
    let locationsSubscription = this.http.get<string>("Mod/ModLoaderInstalled")
      .subscribe(
        B => {
          this.IsModLoaderInstalled = B;
          if(B) this.ModInfos();
        },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
  }

  InstallModLoader() {
    this.YesNo.openDialog({ title: "ModLoader", question: (this.IsModLoaderInstalled ? "Update" : "Install") + " ModLoader?" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        let locationsSubscription = this.http.get<boolean>("Mod/InstallModLoader")
          .subscribe(
            B => {
              this.ModLoaderInstalled();
              this.ModInfos();
            },
            error => this.error = error // error path
          );
        // Stop listening for location after 10 seconds
        setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
      });
  }

  DeleteAllMods() {
    this.YesNo.openDialog({ title: "!!! Delete all Mods from ModLoader !!!", question: "#" + this.Mods.filter(M => M.active).length + " active and remove ModLoader?" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        let locationsSubscription = this.http.get<boolean>("Mod/DeleteAllMods")
          .subscribe(
            B => this.ModLoaderInstalled(),
            error => this.error = error // error path
          );
        // Stop listening for location after 10 seconds
        setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
      });
  }

  ModInfos() {
    let locationsSubscription = this.http.get<ModData[]>("Mod/ModInfos")
      .subscribe(
        M => this.Mods = M,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    let locationsSubscription2 = this.http.get<boolean>("Mod/ModsStarted")
      .subscribe(
        B => this.ModsStarted = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription2.unsubscribe(); }, 120000);
  }

  StartMods() {
    this.YesNo.openDialog({ title: "Start all Mods", question: "#" + this.Mods.filter(M => M.active).length + " active" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get("Mod/StartMods").subscribe(M => this.ModInfos(), error => this.error = error);
      });
  }

  StopMods() {
    this.YesNo.openDialog({ title: "Stop all Mods", question: "#" + this.Mods.filter(M => M.active).length + " active" }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get("Mod/StopMods").subscribe(M => this.ModInfos(), error => this.error = error);
      });
  }

  Save() {
    this.http.post("Mod/ModInfos", this.Mods)
      .subscribe(
        M => this.ModInfos(),
        error => this.error = error // error path
      );
  }

  DeleteMod(aMod: ModData) {
    this.YesNo.openDialog({ title: "Delete Mod", question: aMod.name }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.post("Mod/DeleteMod", aMod).subscribe(M => this.ModInfos(), error => this.error = error);
      });
  }

  set SelectedModConfig(selected: ModData) {
    this._SelectedModConfig = selected;
    this.ShortModName = selected.name;

    if (!this.ShortModName) return;
    let p = this.ShortModName.lastIndexOf(".");
    if(p >= 0) this.ShortModName = this.ShortModName.substr(0, p);

    p = Math.max(this.ShortModName.lastIndexOf("/"), this.ShortModName.lastIndexOf("\\"));
    if (p >= 0) this.ShortModName = this.ShortModName.substr(p + 1);
  }

  onUploaded() {
    this.ModInfos();
  }
}
