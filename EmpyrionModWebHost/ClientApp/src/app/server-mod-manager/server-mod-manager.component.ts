import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';

export class ModData {
  name: string;
  possibleNames: string[];
  active: boolean;
}

@Component({
  selector: 'app-server-mod-manager',
  templateUrl: './server-mod-manager.component.html',
  styleUrls: ['./server-mod-manager.component.less']
})
export class ServerModManagerComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  Mods: ModData[] = [];
  error: any;
  IsModLoaderInstalled: boolean;
    ModsStarted: boolean;

  constructor(
    private http: HttpClient,
  ) { }

  ngOnInit() {
    this.ModLoaderInstalled();
  }

  ModLoaderInstalled() {
    let locationsSubscription = this.http.get<boolean>("Mod/ModLoaderInstalled")
      .subscribe(
        B => {
          this.IsModLoaderInstalled = B;
          if(B) this.ModInfos();
        },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }

  InstallModLoader() {
    let locationsSubscription = this.http.get<boolean>("Mod/InstallModLoader")
      .subscribe(
        B => {
          this.ModLoaderInstalled();
          this.ModInfos();
        },
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);
  }


  ModInfos() {
    let locationsSubscription = this.http.get<ModData[]>("Mod/ModInfos")
      .subscribe(
        M => this.Mods = M,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

    let locationsSubscription2 = this.http.get<boolean>("Mod/ModsStarted")
      .subscribe(
        B => this.ModsStarted = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription2.unsubscribe(); }, 10000);
  }

  StartMods() {
    this.YesNo.openDialog({ title: "Start all Mods", question: "#" + this.Mods.length }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;
        this.http.get("Mod/StartMods").subscribe(M => this.ModInfos(), error => this.error = error);
      });
  }

  StopMods() {
    this.YesNo.openDialog({ title: "Stop all Mods", question: "#" + this.Mods.length }).afterClosed().subscribe(
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

  onUploaded() {
    this.ModInfos();
  }
}
