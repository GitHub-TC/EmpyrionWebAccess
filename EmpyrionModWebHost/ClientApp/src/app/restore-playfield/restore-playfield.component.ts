import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { YesNoDialogComponent, YesNoData } from '../yes-no-dialog/yes-no-dialog.component';
import { GlobalStructureInfo } from '../model/structure-model';
import { StructureService } from '../services/structure.service';

@Component({
  selector: 'app-restore-playfield',
  templateUrl: './restore-playfield.component.html',
  styleUrls: ['./restore-playfield.component.less']
})
export class RestorePlayfieldComponent implements OnInit {
  @ViewChild(YesNoDialogComponent) YesNo: YesNoDialogComponent;
  Backups: string[];
  error: any;
  mSelectedBackup: string;
  Playfields: string[] = [];
  mSelectedPlayfield: string;
  mBackupStructures: GlobalStructureInfo[];
  mAllStructures: GlobalStructureInfo[];
  FailedStructures: GlobalStructureInfo[];

  constructor(
    private http: HttpClient,
    private mStructureService: StructureService,
  ) { }

  ngOnInit() {
    let locationsSubscription = this.http.get<string[]>("Backups/GetBackups")
      .pipe()
      .subscribe(
        B => this.Backups = B,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
    this.mStructureService.GetGlobalStructureList()
      .subscribe(S => this.mAllStructures = S );
  }

  get SelectedBackup() {
    return this.mSelectedBackup;
  }

  set SelectedBackup(aBackup: string) {
    this.mSelectedBackup = aBackup;
    let locationsSubscription = this.http.post<string[]>("Backups/ReadPlayfields", { backup: this.SelectedBackup })
      .pipe()
      .subscribe(
        P => this.Playfields = P,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    let locationsSubscription2 = this.http.post<any>("Backups/ReadStructures", { backup: this.SelectedBackup })
      .pipe()
      .subscribe(
        S => this.mBackupStructures = S,
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription2.unsubscribe(); }, 120000);
  }

  get SelectedPlayfield() {
    return this.mSelectedPlayfield;
  }

  set SelectedPlayfield(aSelectedPlayfield: string) {
    this.mSelectedPlayfield = aSelectedPlayfield;
    this.CheckStructuresIntegrity();
  }

  CheckStructuresIntegrity() {
    this.FailedStructures = this.mBackupStructures
      .filter(S => S.playfield == this.mSelectedPlayfield)
      .map(S => this.mAllStructures.find(s => s.id == S.id && s.playfield != S.playfield))
      .filter(S => !!S);
  }

  RestorePlayfield() {
    this.YesNo.openDialog({ title: "Restore playfied", question: this.SelectedPlayfield }).afterClosed().subscribe(
      (YesNoData: YesNoData) => {
        if (!YesNoData.result) return;

        let locationsSubscription = this.http.post("Backups/RestorePlayfield",
          { backup: this.SelectedBackup, playfield: this.SelectedPlayfield })
          .pipe()
          .subscribe(
            P => { },
            error => this.error = error // error path
          );
        // Stop listening for location after 10 seconds
        setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);
      });
  }
  
}
